using System.ComponentModel;

namespace DaoLang.Shared.Enums
{
    public enum LanguageType
    {
        [Description("英语（美国）")]
        EN_US = 0,

        [Description("英语（加拿大）")]
        EN_CA,

        [Description("中文（中国大陆）")]
        ZH_CN,

        [Description("中文（中国台湾）")]
        ZH_TW,

        [Description("中文（中国香港）")]
        ZH_HK,

        [Description("中文（新加坡）")]
        ZH_SG,

        [Description("阿拉伯语（沙特阿拉伯）")]
        AR_SA,

        [Description("阿拉伯语（伊拉克）")]
        AR_IQ,

        [Description("阿拉伯文（卡塔尔）")]
        AR_QA,

        [Description("朝鲜语（韩国）")]
        KO_KR,

        [Description("日语（日本）")]
        JA_JP,

        [Description("德语（德国）")]
        DE_DE,
    }
}
