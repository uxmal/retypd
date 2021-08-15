using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retypd
{
    public static class Extensions
    {
        public static V get<K, V>(this IDictionary<K,V> self, K key, V? defaultValue = default)
        {
            if (self.TryGetValue(key, out var result))
                return result;
            return defaultValue!;
        }

        public static V get<K, V>(this Dictionary<K, V> self, K key, V? defaultValue = default)
            where K : notnull
        {
            if (self.TryGetValue(key, out var result))
                return result;
            return defaultValue!;
        }
    }
}
