using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiHtmlVideoInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);


        [JsonPropertyName("albumQipuId")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("tvid")]
        public Int64 TvId { get; set; }
        [JsonPropertyName("videoName")]
        public String VideoName { get; set; }
        [JsonPropertyName("videoUrl")]
        public String VideoUrl { get; set; }
        [JsonPropertyName("channelName")]
        public string ChannelName { get; set; }
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonIgnore]
        public List<IqiyiEpisode> Epsodelist { get; set; }

        [JsonIgnore]
        public IqiyiHtmlAlbumInfo? AlbumInfo { get; set; }

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

    }
}
