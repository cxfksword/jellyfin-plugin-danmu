using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core
{
    public static class Utils
    {
        // 北京时区
        static TimeZoneInfo beijingTimeZone = TimeZoneInfo.CreateCustomTimeZone("GMT+8", TimeSpan.FromHours(8), null, null);

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // 转成北京时间，bilibili接口的年份只要是根据北京时间返回
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = TimeZoneInfo.ConvertTime(dateTime.AddSeconds(unixTimeStamp), TimeZoneInfo.Utc, beijingTimeZone);
            return dateTime;
        }
    }
}
