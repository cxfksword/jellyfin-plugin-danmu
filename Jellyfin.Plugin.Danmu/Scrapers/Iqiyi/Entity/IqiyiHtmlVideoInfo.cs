using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiHtmlVideoInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("albumQipuId")]
        public long AlbumId { get; set; }

        [JsonPropertyName("tvid")]
        public long TvId { get; set; }

        [JsonPropertyName("videoName")]
        public string VideoName { get; set; }

        private string _videoUrl;
        [JsonPropertyName("videoUrl")]
        public string VideoUrl
        {
            get
            {
                if (this._videoUrl == null)
                {
                    return string.Empty;
                }
                if (this._videoUrl.StartsWith("http://") || this._videoUrl.StartsWith("https://"))
                {
                    return this._videoUrl;
                }
                if (this._videoUrl.StartsWith("//"))
                {
                    return "https:" + this._videoUrl;
                }
                return this._videoUrl;
            }
            set
            {
                _videoUrl = value;
            }
        }

        // [JsonPropertyName("channelId")]
        // public int channelId { get; set; }

        [JsonPropertyName("channelName")]
        public string channelName { get; set; }

        [JsonPropertyName("duration")]
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

        // [JsonIgnore]
        // public string ChannelName
        // {
        //     get
        //     {
        //         switch (channelId)
        //         {
        //             case 1:
        //                 return "电影";
        //             case 2:
        //                 return "电视剧";
        //             case 3:
        //                 return "纪录片";
        //             case 4:
        //                 return "动漫";
        //             case 6:
        //                 return "综艺";
        //             case 15:
        //                 return "儿童";
        //             default:
        //                 return string.Empty;
        //         }
        //     }
        // }

    }
}
