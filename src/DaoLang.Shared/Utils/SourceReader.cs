using DaoLang.Extensions;
using DaoLang.Shared.Enums;
using DaoLang.Shared.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DaoLang.Shared.Utils
{
    internal static class SourceReader
    {
        public static void Save(Language language, string directory, string flag)
        {
            var filePath = Path.Combine(directory, GetFileName(directory, flag, language.LanguageType));
            Save(language, filePath);
        }

        public static string GetFileName(string directory, string flag, LanguageType language)
        {
            return Path.Combine(directory, $"{flag}.{language.GetCommonName()}.xml");
        }

        public static bool Load(string fileName, out Language language)
        {
            language = default;

            if (File.Exists(fileName))
            {
                try
                {
                    // 不使用File.ReadAllText()防止文件占用
                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 防止存在utf8 bom头导致的反序列化错误
                        using (var reader = new StreamReader(fs, new UTF8Encoding(true)))
                        {
                            var xmlSerializer = new XmlSerializer(typeof(Language));
                            language = xmlSerializer.Deserialize(reader) as Language;
                            if (language != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 保存临时错误文件，防止误覆盖。留给用户自行判断处理，手动清除
                    var errorPath = fileName + ".tmp.err";
                    File.Create(errorPath).Close();
                    File.WriteAllText(errorPath, ex.Message);
                    File.Copy(fileName, fileName + ".tmp", true);
                }
            }

            return false;
        }

        public static bool Load(Assembly assembly, string sourceName, out Language language)
        {
            language = default;

            try
            {
                if (assembly.GetManifestResourceNames()?.Contains(sourceName) == true)
                {
                    using (var stream = assembly.GetManifestResourceStream(sourceName))
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                var xmlSerializer = new XmlSerializer(typeof(Language));
                                language = xmlSerializer.Deserialize(reader) as Language;
                                if (language != null)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO 保存备份资源文件
            }
            return false;
        }

        public static void Save(Language source, string filePath)
        {
            // 如果与文件现有相同，则不再保存
            if (Load(filePath, out var tmpLanguage) && source.Equals(tmpLanguage))
            {
                return;
            }

            var serializer = new XmlSerializer(typeof(Language));
            using (var sw = new StringWriter())
            {

                using (var writer = new XmlTextWriter(sw))
                {


                    writer.Indentation = 2;
                    writer.Formatting = Formatting.Indented;
                    serializer.Serialize(writer, source);
                    var xmlStr = sw.ToString();

                    if (!File.Exists(filePath))
                    {
                        var path = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                    }

                    File.WriteAllText(filePath, xmlStr, Encoding.UTF8);
                }
            }
        }
    }
}
