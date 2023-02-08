using DaoLang.Shared.Enums;

namespace DaoLang.Extensions
{
    internal static class EnumEx
    {
        /// <summary>
        /// 获取LanguageType通用名称
        /// </summary>
        /// <param name="languaeType"></param>
        /// <returns></returns>
        public static string GetCommonName(this LanguageType languaeType)
        {
            var tmp = languaeType.ToString();
            return tmp.ToLower().Replace('_', '-');
        }
    }
}
