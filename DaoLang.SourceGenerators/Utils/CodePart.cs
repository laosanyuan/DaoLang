using System;
using System.Collections.Generic;
using System.Text;

namespace DaoLang.SourceGenerators.Utils
{
    internal class CodePart
    {
        /// <summary>
        /// 换行
        /// </summary>
        public const string Enter = "\n";
        /// <summary>
        /// 缩进
        /// </summary>
        public const string TAB = "    ";

        /// <summary>
        /// 缩进
        /// </summary>
        /// <param name="n">tab数量</param>
        /// <returns>4n个space</returns>
        public static string Tab(int count) => MultiStr(TAB, count);

        /// <summary>
        /// 生成多个标记字符串
        /// </summary>
        /// <param name="singleStr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static string MultiStr(string singleStr, int count)
        {
            var result = string.Empty;
            for (var i = 0; i < count; i++)
            {
                result += singleStr;
            }
            return result;
        }
    }
}
