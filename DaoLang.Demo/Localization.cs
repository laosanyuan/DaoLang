
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace DaoLang.Demo
{
    [MainLanguage("Source","Language",LanguageType.EN_US)]
    [SecondaryLanguage(LanguageType.ZH_CN)]
    public partial class LocalizationTest
    {
        [Entry("Test")]
        private static string _test = "";
        [Entry]
        private static string _test2 = "";
    }
}
