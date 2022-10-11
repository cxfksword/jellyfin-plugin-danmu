using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core
{
    public static class StringExtension
    {
        public static long ToLong(this string s)
        {
            long val;
            if (long.TryParse(s, out val))
            {
                return val;
            }

            return 0;
        }

        public static int ToInt(this string s)
        {
            int val;
            if (int.TryParse(s, out val))
            {
                return val;
            }

            return 0;
        }
    }
}
