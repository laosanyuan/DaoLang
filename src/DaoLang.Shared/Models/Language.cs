using DaoLang.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

#if WPF
using System.Windows;
#elif WinUI3
using Microsoft.UI.Xaml;
#endif

namespace DaoLang.Shared.Models
{
    [Serializable]
    public class Language
    {
        #region [Properties]
        /// <summary>
        /// 语言标识
        /// </summary>
        public LanguageType LanguageType { get; set; }

        /// <summary>
        /// 是否为主语言
        /// </summary>
        public bool IsMainLanguage { get; set; }

        /// <summary>
        /// 词条集合
        /// </summary>
        public List<LanguageItem> Items { get; set; } = new List<LanguageItem>();
        #endregion

        #region [Methods]
        /// <summary>
        /// 词条索引器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public LanguageItem this[int index]
        {
            get
            {
                if (index < 0 || index > Items.Count || Items is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return Items[index];
            }
            set
            {
                if (index >= 0 && index < Items.Count)
                {
                    this.Items[index] = value;
                }
                else if (index == Items.Count)
                {
                    this.Items.Add(value);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        /// <summary>
        /// 词条索引器
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LanguageItem this[string key]
            => ContainsKey(key)
                ? this.Items.FirstOrDefault(t => t.Key.Equals(key))
                : default;

        /// <summary>
        /// 添加词条
        /// </summary>
        /// <param name="item"></param>
        public void Add(LanguageItem item) => this.Items.Add(item);

        /// <summary>
        /// 根据key获取词条
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out LanguageItem entry)
        {
            entry = default;

            if (!string.IsNullOrEmpty(key) && Items.Any(t => t.Key.Equals(key)))
            {
                entry = Items.FirstOrDefault(t => t.Key.Equals(key));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否包含key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return !string.IsNullOrEmpty(key) && Items.Any(t => t.Key.Equals(key));
        }

        public override bool Equals(object obj)
        {
            if (obj is Language target
                && target.LanguageType == this.LanguageType
                && target.IsMainLanguage == this.IsMainLanguage)
            {
                if ((target.Items.Count == 0 && this.Items.Count == 0))
                {
                    return true;
                }

                if (target.Items.Count == this.Items.Count)
                {
                    return target.Items.All(t =>
                        this.Items.Any(s => (s.Key.Equals(t.Key) && s.Content?.Equals(t.Content) == true)));
                }
            }

            return false;
        }

        public override int GetHashCode() => this.Items.GetHashCode() + this.LanguageType.GetHashCode() + this.IsMainLanguage.GetHashCode();

#if WinUI3 || WPF
        public ResourceDictionary ConvertToResourceDictionary(Language backup = null)
        {
            var result = new ResourceDictionary();
            Items?.ForEach(t =>
            {
                // 当词条内容为空时使用备份语言词条内容作为替补
                if (string.IsNullOrEmpty(t.Content) && backup != null)
                {
                    result.Add(t.Key, backup[t.Key].Content);
                }
                else
                {
                    result.Add(t.Key, t.Content);
                }
            });
            return result;
        }
#elif Avalonia
        public Dictionary<string, string> ConvertToDictionary(Language backup = null)
        {
            var result = new Dictionary<string, string>();
            Items?.ForEach(t =>
            {
                // 当词条内容为空时使用备份语言词条内容作为替补
                if (string.IsNullOrEmpty(t.Content) && backup != null)
                {
                    result.Add(t.Key, backup[t.Key].Content);
                }
                else
                {
                    result.Add(t.Key, t.Content);
                }
            });
            return result;
        }
#endif

        #endregion
    }

    /// <summary>
    /// 词条
    /// </summary>
    [Serializable]
    public class LanguageItem
    {
        /// <summary>
        /// 资源Key
        /// </summary>
        [XmlAttribute]
        public string Key { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [XmlAttribute]
        public string Content { get; set; } = string.Empty;
    }
}
