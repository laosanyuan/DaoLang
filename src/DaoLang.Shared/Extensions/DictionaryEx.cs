using System;
using System.Collections.Generic;
using System.Linq;

namespace DaoLang.Shared.Extensions
{
    internal static class DictionaryEx
    {
        /// <summary>
        /// 判断两个Dictionary字典内容是否相同
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsSame<T1, T2>(this Dictionary<T1, T2> source, Dictionary<T1, T2> target)
            where T1 : class
            where T2 : class
        {
            if (source != null
                && target != null
                && source.Count == target.Count)
            {
                var result = target.Where(x => !source.ContainsKey(x.Key) || !source[x.Key].Equals(x.Value))
                    .Union(source.Where(x => !target.ContainsKey(x.Key) || !target[x.Key].Equals(x.Value)))
                    .Distinct()
                    .ToDictionary(x => x.Key, x => x.Value);
                return result.Count == 0;
            }

            return false;
        }

        /// <summary>
        /// 判断两个Dictonary的key集合是否相同
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsKeySame<T1, T2>(this Dictionary<T1, T2> source, Dictionary<T1, T2> target)
            where T2 : class, IEquatable<T2>
        {
            if (source != null
                && target != null
                && source.Count == target.Count)
            {
                var result = target.Where(x => !source.ContainsKey(x.Key))
                    .Union(source.Where(x => !target.ContainsKey(x.Key)))
                    .ToDictionary(x => x.Key, x => x.Value);
                return result.Count == 0;
            }

            return false;
        }
    }
}
