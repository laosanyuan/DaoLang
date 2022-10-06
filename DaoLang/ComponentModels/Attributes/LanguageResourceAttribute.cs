namespace DaoLang.ComponentModels.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class LanguageResourceAttribute : Attribute
    {
        /// <summary>
        /// 主要语言
        /// </summary>
        public string MajorLanugae { get; set; } = "en-US";
        /// <summary>
        /// 次要语言列表
        /// </summary>
        public IList<string> SecondaryLanguages { get; set; } = null!;
        /// <summary>
        /// 创建文件名称
        /// </summary>
        public string FileName { get; set; } = "Language";


        public LanguageResourceAttribute(string majorLanugae = null!, IList<string> secondaryLanguages = null!, string fileName = null!)
        {
            if (!string.IsNullOrEmpty(majorLanugae))
            {
                MajorLanugae = majorLanugae;
            }
            SecondaryLanguages = secondaryLanguages;
            if (!string.IsNullOrEmpty(fileName))
            {
                FileName = fileName;
            }
        }

        public LanguageResourceAttribute()
        { 
        }
    }
}
