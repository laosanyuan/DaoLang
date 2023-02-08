using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace DaoLang.Demo
{
    [MainLanguage("Source", "Language", LanguageType.EN_US)]
    [SecondaryLanguage(LanguageType.ZH_CN)]
    [SecondaryLanguage(LanguageType.ZH_TW)]
    [SecondaryLanguage(LanguageType.AR_SA)]
    [SecondaryLanguage(LanguageType.KO_KR)]
    [SecondaryLanguage(LanguageType.JA_JP)]
    [SecondaryLanguage(LanguageType.DE_DE)]
    public partial class Localization
    {
        [Entry("中文")]
        private static string _chinese = "Chinese";

        [Entry("中文繁体")]
        private static string _chineseTw = "Chinese TW";

        [Entry("英语")]
        private static string _english = "English";

        [Entry("阿拉伯语")]
        private static string _arab = "Arab";

        [Entry("日语")]
        private static string _japanese = "Japanese";

        [Entry("韩语")]
        private static string _korean = "Korean";

        [Entry("德语")]
        private static string _german = "German";
    }
}
