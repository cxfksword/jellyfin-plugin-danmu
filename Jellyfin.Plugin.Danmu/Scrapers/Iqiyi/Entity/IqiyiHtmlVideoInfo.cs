using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiHtmlVideoInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("albumId")]
        public long AlbumId { get; set; }
        [JsonPropertyName("tvId")]
        public long TvId { get; set; }
        [JsonPropertyName("name")]
        public string VideoName { get; set; }
        [JsonPropertyName("playUrl")]
        public string VideoUrl { get; set; }
        [JsonPropertyName("channelId")]
        public int channelId { get; set; }
        [JsonPropertyName("durationSec")]
        public int Duration { get; set; }
        [JsonPropertyName("videoCount")]
        public int VideoCount { get; set; }

        [JsonIgnore]
        public List<IqiyiEpisode> Epsodelist { get; set; }

        [JsonIgnore]
        public string LinkId
        {
            get
            {
                var match = regLinkId.Match(VideoUrl);
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

        [JsonIgnore]
        public string ChannelName
        {
            get
            {
                switch (channelId)
                {
                    case 1:
                        return "电影";
                    case 2:
                        return "电视剧";
                    case 3:
                        return "纪录片";
                    case 4:
                        return "动漫";
                    case 6:
                        return "综艺";
                    case 15:
                        return "儿童";
                    default:
                        return string.Empty;
                }
            }
        }

    }
}
