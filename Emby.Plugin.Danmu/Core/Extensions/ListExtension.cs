using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class ListExtension
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));

        /// <summary>
        /// 从list抽取间隔指定大小数量的item
        /// </summary>
        public static IEnumerable<T> ExtractToNumber<T>(this IEnumerable<T> self, int limit)
        {

            var count = self.Count();
            var step = (int)Math.Ceiling((double)count / limit);
            var list = new List<T>();
            var idx = 0;
            for (var i = 0; i < limit; i++)
            {
                if (idx >= count)
                {
                    break;
                }
                list.Add(self.ElementAt(idx));
                idx += step;
            }

            return list;
        }
    }
}