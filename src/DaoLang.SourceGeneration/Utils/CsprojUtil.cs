using DaoLang.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;

namespace DaoLang.SourceGeneration.Utils
{
    internal static class CsprojUtil
    {
        private const string CleanupTargetName = "DaoLangCleanupGeneratedResources";
        private const string CleanupDesignTimeTargetName = "DaoLangCleanupGeneratedResourcesDesignTime";
        private const string CleanupManifestRelativePath = @"obj\DaoLang.GeneratedResourceCleanup.txt";

        /// <summary>
        /// 向项目文件中添加复制输出文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="outputFiles"></param>
        /// <param name="generationType"></param>
        public static void AddFileToOutput(
            string fileName,
            List<string> outputFiles,
            FileGenerationType generationType = FileGenerationType.OutputDirectory)
        {
            if (!fileName.Contains(".csproj"))
            {
                return;
            }
            var xmldoc = new XmlDocument();
            xmldoc.Load(fileName);
            XmlNode? targetNode = default;
            var duplicateNodes = new List<XmlNode>();

            // 获取csproj文件中的有效ItemGroup，即包含资源文件元素的节点
            var nodes = xmldoc.SelectNodes("//ItemGroup");
            if (nodes is not null)
            {
                foreach (XmlNode item in nodes)
                {
                    if (!ContainsGeneratedLanguageElement(item))
                    {
                        continue;
                    }

                    if (targetNode is null)
                    {
                        targetNode = item;
                    }
                    else
                    {
                        duplicateNodes.Add(item);
                    }
                }
            }

            // 首次新增
            if (targetNode is null)
            {
                targetNode = xmldoc.CreateElement("ItemGroup");
                var comment = xmldoc.CreateComment("本节点由DaoLang自动生成，请勿手动修改");
                xmldoc.DocumentElement?.AppendChild(comment);
                xmldoc.DocumentElement?.AppendChild(targetNode);
            }
            else
            {
                foreach (var duplicateNode in duplicateNodes)
                {
                    duplicateNode.ParentNode?.RemoveChild(duplicateNode);
                }
            }

            EnsureCleanupTarget(xmldoc);
            targetNode.RemoveAll();

            foreach (var element in outputFiles.Select(file => CreateLanguageElement(xmldoc, generationType, file)).Where(element => element != null))
            {
                targetNode.AppendChild(element);
            }

            var updatedContent = SerializeXmlDocument(xmldoc);
            var existingContent = File.Exists(fileName)
                ? File.ReadAllText(fileName)
                : string.Empty;

            if (string.Equals(existingContent, updatedContent, StringComparison.Ordinal))
            {
                return;
            }

            WriteAllTextAtomically(fileName, updatedContent);
        }

        public static void WriteCleanupManifest(string projectDirectory, IEnumerable<string> staleFiles)
        {
            var manifestPath = Path.Combine(projectDirectory, CleanupManifestRelativePath);
            var content = staleFiles.Any()
                ? string.Join(Environment.NewLine, staleFiles) + Environment.NewLine
                : string.Empty;

            var directory = Path.GetDirectoryName(manifestPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existingContent = File.Exists(manifestPath)
                ? File.ReadAllText(manifestPath)
                : string.Empty;

            if (string.Equals(existingContent, content, StringComparison.Ordinal))
            {
                return;
            }

            WriteAllTextAtomically(manifestPath, content);
        }

        private static bool ContainsGeneratedLanguageElement(XmlNode itemGroup)
        {
            const string pattern = "^[\\w\\\\]+[\\.][\\w]{2}([_-])[\\w]{2}.xml$";

            foreach (XmlNode child in itemGroup.ChildNodes)
            {
                if (child.Attributes is null)
                {
                    continue;
                }

                var update = child.Attributes["Update"]?.Value;
                var include = child.Attributes["Include"]?.Value;

                if ((!string.IsNullOrEmpty(update) && Regex.IsMatch(update, pattern))
                    || (!string.IsNullOrEmpty(include) && Regex.IsMatch(include, pattern)))
                {
                    return true;
                }
            }

            return false;
        }

        // 创建语言ItemGroup生成节点
        private static XmlElement CreateLanguageElement(XmlDocument document, FileGenerationType type, string file)
        {
            XmlElement element = null!;
            switch (type)
            {
                case FileGenerationType.OutputDirectory:
                    element = document.CreateElement("None");
                    element.SetAttribute("Update", file);
                    var childOutput = document.CreateElement("CopyToOutputDirectory");
                    childOutput.InnerText = "PreserveNewest";
                    element.AppendChild(childOutput);
                    break;
                case FileGenerationType.Embedded:
                    element = document.CreateElement("EmbeddedResource");
                    element.SetAttribute("Include", file);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return element;
        }

        private static string SerializeXmlDocument(XmlDocument document)
        {
            using var stream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(false)
            });
            document.Save(xmlWriter);
            xmlWriter.Flush();
            stream.Position = 0;
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            return reader.ReadToEnd();
        }

        private static void EnsureCleanupTarget(XmlDocument document)
        {
            var targetNode = document.SelectSingleNode($"/Project/Target[@Name='{CleanupTargetName}']");
            var designTimeTargetNode = document.SelectSingleNode($"/Project/Target[@Name='{CleanupDesignTimeTargetName}']");
            if (targetNode != null && designTimeTargetNode != null)
            {
                return;
            }

            if (targetNode == null)
            {
                var target = CreateCleanupTarget(document, CleanupTargetName, "Build", "'$(DesignTimeBuild)' != 'true'");
                document.DocumentElement?.AppendChild(target);
            }

            if (designTimeTargetNode == null)
            {
                var designTimeTarget = CreateCleanupTarget(document, CleanupDesignTimeTargetName, "CoreCompile", "'$(DesignTimeBuild)' == 'true'");
                document.DocumentElement?.AppendChild(designTimeTarget);
            }
        }

        private static void WriteAllTextAtomically(string fileName, string content)
        {
            var tempFileName = fileName + ".tmp";
            File.WriteAllText(tempFileName, content, new UTF8Encoding(false));

            if (File.Exists(fileName))
            {
                File.Copy(tempFileName, fileName, true);
                File.Delete(tempFileName);
                return;
            }

            File.Move(tempFileName, fileName);
        }

        private static XmlElement CreateCleanupTarget(XmlDocument document, string targetName, string afterTargets, string condition)
        {
            var target = document.CreateElement("Target");
            target.SetAttribute("Name", targetName);
            target.SetAttribute("AfterTargets", afterTargets);
            target.SetAttribute("Condition", condition);

            var readLines = document.CreateElement("ReadLinesFromFile");
            readLines.SetAttribute("File", $@"$(MSBuildProjectDirectory)\{CleanupManifestRelativePath}");
            readLines.SetAttribute("Condition", $@"Exists('$(MSBuildProjectDirectory)\{CleanupManifestRelativePath}')");

            var output = document.CreateElement("Output");
            output.SetAttribute("TaskParameter", "Lines");
            output.SetAttribute("ItemName", "_DaoLangFilesToDelete");
            readLines.AppendChild(output);

            var delete = document.CreateElement("Delete");
            delete.SetAttribute("Files", "@(_DaoLangFilesToDelete)");
            delete.SetAttribute("Condition", "'@(_DaoLangFilesToDelete)' != ''");
            delete.SetAttribute("ContinueOnError", "WarnAndContinue");

            var deleteManifest = document.CreateElement("Delete");
            deleteManifest.SetAttribute("Files", $@"$(MSBuildProjectDirectory)\{CleanupManifestRelativePath}");
            deleteManifest.SetAttribute("Condition", $@"Exists('$(MSBuildProjectDirectory)\{CleanupManifestRelativePath}')");
            deleteManifest.SetAttribute("ContinueOnError", "WarnAndContinue");

            target.AppendChild(readLines);
            target.AppendChild(delete);
            target.AppendChild(deleteManifest);
            return target;
        }
    }
}
