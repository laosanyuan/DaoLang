using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace DaoLang.SourceGenerators.Utils
{
    internal static class CsprojUtil
    {
        /// <summary>
        /// 向项目文件中添加复制输出文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="outputFiles"></param>
        public static void AddFileToOutput(string fileName, List<string> outputFiles)
        {
            if (!fileName.Contains(".csproj"))
            {
                return;
            }
            var xmldoc = new XmlDocument();
            xmldoc.Load(fileName);
            XmlNode targetNode = default!;

            // 获取csproj文件中的有效ItemGroup，即包含资源文件元素的
            var nodes = xmldoc.SelectNodes("//ItemGroup");
            if (nodes is not null)
            {
                foreach (XmlNode item in nodes)
                {
                    var update = item.FirstChild?.Attributes?["Update"];

                    if (string.IsNullOrEmpty(update?.Value))
                    {
                        continue;
                    }

                    // 按文件名格式检查匹配
                    if (!Regex.IsMatch(update!.Value, "^[\\w\\\\]+[\\.][\\w]{2}-[\\w]{2}.xml$"))
                    {
                        continue;
                    }

                    targetNode = item;
                    break;
                }
            }

            // 首次新增
            if (targetNode is null)
            {
                targetNode = xmldoc.CreateElement("ItemGroup");
                var comment = xmldoc.CreateComment("本节点由DaoLang自动生成，请勿手动修改");
                xmldoc.DocumentElement.AppendChild(comment);
                xmldoc.DocumentElement.AppendChild(targetNode);
            }

            // 已存在的不处理
            foreach (XmlNode node in targetNode.ChildNodes)
            {
                if (outputFiles.Contains(node?.Attributes?["Update"]?.Value ?? string.Empty))
                {
                    outputFiles.Remove(node!.Attributes!["Update"]!.Value!);
                }
            }

            foreach (var file in outputFiles)
            {
                // 新增节点指定复制文件
                var element = xmldoc.CreateElement("None");
                element.SetAttribute("Update", file);
                var child = xmldoc.CreateElement("CopyToOutputDirectory");
                child.InnerText = "PreserveNewest";
                element.AppendChild(child);
                targetNode.AppendChild(element);
            }

            // 保存更新
            if (outputFiles.Count > 0)
            {
                using var stream = new FileStream(fileName, FileMode.OpenOrCreate);
                xmldoc.Save(stream);
            }
        }
    }
}
