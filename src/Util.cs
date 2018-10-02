namespace Ghi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;

    internal static class Util
    {
        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
            dict.TryGetValue(key, out V holder);
            return holder;
        }

        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key, V def)
        {
            if (dict.TryGetValue(key, out V holder))
            {
                return holder;
            }

            return def;
        }

        internal static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<Tuple<K, V>> enumerable)
        {
            return enumerable.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
        }
    }
}
