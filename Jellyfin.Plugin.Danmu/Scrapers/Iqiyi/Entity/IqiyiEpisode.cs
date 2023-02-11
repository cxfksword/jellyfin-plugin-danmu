using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiEpisode
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);


        [JsonPropertyName("tvId")]
        public Int64 TvId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("playUrl")]
        public string PlayUrl { get; set; }


        public int TotalMat
        {
            get
            {
                if (Duration.Length == 5 && TimeSpan.TryParseExact(Duration, @"mm\:ss", null, out var duration))
                {
                    return (int)Math.Floor(duration.TotalSeconds / 300) + 1;
                }

                if (Duration.Length == 8 && TimeSpan.TryParseExact(Duration, @"hh\:mm\:ss", null, out var durationHour))
                {
                    return (int)Math.Floor(durationHour.TotalSeconds / 300) + 1;
                }

                return 0;
            }

        }

        public string LinkId
        {
            get
            {
                var match = regLinkId.Match(PlayUrl);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
