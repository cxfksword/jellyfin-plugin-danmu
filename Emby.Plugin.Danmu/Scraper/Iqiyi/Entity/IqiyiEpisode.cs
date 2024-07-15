using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiEpisode
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [DataMember(Name="tvId")]
        public Int64 TvId { get; set; }

        [DataMember(Name="name")]
        public string Name { get; set; }

        [DataMember(Name="order")]
        public int Order { get; set; }

        [DataMember(Name="duration")]
        public string Duration { get; set; }

        [DataMember(Name="playUrl")]
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