using DaoLang.Shared.Enums;
using System;

namespace DaoLang.Attributes
{
    /// <summary>
    /// 主语言标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MainLanguageAttribute : Attribute
    {
        /// <summary>
        /// 语言类别
        /// </summary>
        public LanguageType LanguageType { get; }
        /// <summary>
        /// 语言文件路径
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// 语言文件名称标题
        /// 生成格式：
        ///     FileName.en-us.xml
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// 资源文件输出方式
        /// </summary>
        public FileGenerationType FileGenerationType { get; }

#if MAUI
        public MainLanguageAttribute(string path, string fileName, LanguageType languageType)
        {
            LanguageType = languageType;
            Path = path;
            FileName = fileName;
        }
#else
        public MainLanguageAttribute(
            string path,
            string fileName,
            LanguageType languageType,
            FileGenerationType fileGenerationType = FileGenerationType.OutputDirectory)
        {
            LanguageType = languageType;
            Path = path;
            FileName = fileName;
            FileGenerationType = fileGenerationType;
        }
#endif
    }

    /// <summary>
    /// 次语言标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SecondaryLanguageAttribute : Attribute
    {
        /// <summary>
        /// 语言类别
        /// </summary>
        public LanguageType LanguageType { get; }

        public SecondaryLanguageAttribute(LanguageType languageType)
        {
            this.LanguageType = languageType;
        }
    }
}
