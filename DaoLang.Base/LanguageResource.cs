using DaoLang.Extensions;
using DaoLang.Shared.Enums;
using DaoLang.Shared.Models;
using DaoLang.Shared.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace DaoLang
{
    public abstract partial class LanguageResource
    {
        #region [Fields]
        /// <summary>
        /// 资源类类型
        /// </summary>
        protected static Type _sourceType;
        /// <summary>
        /// 资源文件文件夹路径
        /// </summary>
        protected static string _folder;
        /// <summary>
        /// 文件标识名称
        /// </summary>
        protected static string _fileFlag;
        /// <summary>
        /// 主语言
        /// </summary>
        protected static LanguageType _mainLanguage;
        /// <summary>
        /// 副语言集合
        /// </summary>
        protected static LanguageType[] _secondaryLanguages;
        /// <summary>
        /// 主语言资源
        /// </summary>
        protected static Language _mainSource;
        #endregion

        #region [Events]
        public delegate void LanguageDelegate(LanguageEventArgs args);
        /// <summary>
        /// 语言变更通知WPF程序切换资源
        /// </summary>
        public static event LanguageDelegate LanguageChanged = null;
        #endregion

        #region [Public Methods]
        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static bool SetLanguage(LanguageType language)
        {
            if (_secondaryLanguages?.Contains(language) == true)
            {
                _mainSource ??= PropertyToSource(_mainLanguage);
                LoadLanguageSource(language);
            }
            else
            {
                SetMainLanguage();
            }
            return false;
        }

        /// <summary>
        /// 设置为主语言
        /// </summary>
        public static void SetMainLanguage() => LoadLanguageSource(_mainLanguage);
        #endregion

        #region [Private Methods]
        /// <summary>
        /// 根据字段名称生成对应的属性名
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns></returns>
        protected static string GetPropertyName(string fieldName)
        {
            var propertyName = fieldName;
            if (propertyName.StartsWith("_"))
            {
                propertyName = propertyName.TrimStart('_');
            }

            return $"{char.ToUpper(propertyName[0], CultureInfo.InvariantCulture)}{propertyName[1..]}";
        }

        /// <summary>
        /// 获取资源文件名
        /// </summary>
        /// <param name="languageType"></param>
        /// <returns></returns>
        protected static string GetSourceFileName(LanguageType languageType)
        {
            var fileName = Path.Combine(_folder, $"{_fileFlag}.{languageType.GetCommonName()}.xml");
            return Path.Combine(Environment.CurrentDirectory, fileName);
        }

        /// <summary>
        /// 资源转换为属性
        /// </summary>
        /// <param name="language"></param>
        protected static void SourceToProperty(Language language)
        {
            if (language is null)
            {
                return;
            }

            foreach (var field in _sourceType.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                var propertyName = GetPropertyName(field.Name);
                if (language.TryGetValue(propertyName, out var sentence) && typeof(string) == field.FieldType)
                {
                    // 当前资源没有设置词条时，使用主语言资源词条
                    if (!string.IsNullOrEmpty(sentence.Content))
                    {
                        field.SetValue(_sourceType, sentence.Content);
                    }
                    else if (_mainSource?.TryGetValue(propertyName, out var mainEntry) == true)
                    {
                        field.SetValue(_sourceType, mainEntry.Content);
                    }
                }
            }
        }

        /// <summary>
        /// 属性转换资源
        /// </summary>
        /// <param name="languageType"></param>
        /// <returns></returns>
        protected static Language PropertyToSource(LanguageType languageType)
        {
            Language language = new() { LanguageType = languageType };

            foreach (var property in _sourceType.GetProperties())
            {
                var content = languageType == _mainLanguage ? property.GetValue(_sourceType) as string : string.Empty;
                language.Add(new LanguageItem() { Key = property.Name, Content = content });
            }

            return language;
        }

        /// <summary>
        /// 加载语言资源文件
        /// </summary>
        /// <param name="languageType"></param>
        /// <returns></returns>
        protected static bool LoadLanguageSource(LanguageType languageType)
        {
            var fileName = GetSourceFileName(languageType);
            if (SourceReader.Load(fileName, out var language))
            {
                SourceToProperty(language);
                // 通知WPF程序资源变更，当前语言结合主语言
                LanguageChanged?.Invoke(new LanguageEventArgs()
                {
                    LanguageType = language.LanguageType,
                    ResourceDictionary = language.ConvertToResourceDictionary(_mainSource)
                });
                return true;
            }
            return false;
        }
        #endregion
    }

    public class LanguageEventArgs : EventArgs
    {
        public LanguageType LanguageType { get; set; }
        public ResourceDictionary ResourceDictionary { get; set; }
    }
}
