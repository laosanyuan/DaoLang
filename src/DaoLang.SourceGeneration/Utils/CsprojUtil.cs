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

            // 获取csproj文件中的有效ItemGroup，即包含资源文件元素的节点
            var nodes = xmldoc.SelectNodes("//ItemGroup");
            if (nodes is not null)
            {
                foreach (XmlNode item in nodes)
                {
                    var update = item.FirstChild?.Attributes?["Update"];
                    var include = item.FirstChild?.Attributes?["Include"];

                    if (string.IsNullOrEmpty(update?.Value) && string.IsNullOrEmpty(include?.Value))
                    {
                        continue;
                    }

                    var pattern = "^[\\w\\\\]+[\\.][\\w]{2}([_-])[\\w]{2}.xml$";
                    if (!string.IsNullOrEmpty(update?.Value) && Regex.IsMatch(update!.Value, pattern))
                    {
                        targetNode = item;
                        break;
                    }

                    if (!string.IsNullOrEmpty(include?.Value) && Regex.IsMatch(include!.Value, pattern))
                    {
                        targetNode = item;
                        break;
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

            targetNode.RemoveAll();

            foreach (var element in outputFiles.Select(file => CreateLanguageElement(xmldoc, generationType, file)).Where(element => element != null))
            {
                targetNode.AppendChild(element);
            }

            // 保存更新
            if (outputFiles.Count > 0)
            {
                var updatedContent = SerializeXmlDocument(xmldoc);
                var existingContent = File.Exists(fileName)
                    ? File.ReadAllText(fileName)
                    : string.Empty;

                if (string.Equals(existingContent, updatedContent, StringComparison.Ordinal))
                {
                    return;
                }

                File.WriteAllText(fileName, updatedContent, new UTF8Encoding(false));
            }
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
    }
}
