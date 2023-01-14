using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core
{
    public static class Utils
    {
        private static TimeZoneInfo beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = TimeZoneInfo.ConvertTime(dateTime.AddSeconds(unixTimeStamp), TimeZoneInfo.Utc, beijingTimeZone);
            return dateTime;
        }
    }
}
