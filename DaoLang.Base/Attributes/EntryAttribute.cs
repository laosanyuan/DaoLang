using System;

namespace DaoLang.Attributes
{
    /// <summary>
    /// 词条标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EntryAttribute : Attribute
    {
        /// <summary>
        /// 注释
        /// </summary>
        public string Comment { get; set; }

        public EntryAttribute(string comment = null!)
        {
            Comment = comment;
        }
    }
}
