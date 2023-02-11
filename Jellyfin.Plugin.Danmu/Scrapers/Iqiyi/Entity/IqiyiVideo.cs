using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiVideo
    {
        private static readonly Regex regLinkId = new Regex(@"(v_|a_)(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("albumId")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("tvId")]
        public Int64 TvId { get; set; }
        [JsonPropertyName("playUrl")]
        public string PlayUrl { get; set; }
        [JsonPropertyName("albumUrl")]
        public string AlbumUrl { get; set; }
        [JsonPropertyName("name")]
        public String Name { get; set; }
        [JsonPropertyName("channelId")]
        public int ChannelId { get; set; }
        [JsonPropertyName("videoCount")]
        public int VideoCount { get; set; }
        [JsonPropertyName("duration")]
        public String Duration { get; set; }
        [JsonPropertyName("publishTime")]
        public Int64 publishTime { get; set; }

        [JsonPropertyName("epsodelist")]
        public List<IqiyiEpisode> Epsodelist { get; set; }


        public string LinkId
        {
            get
            {
                var match = regLinkId.Match(AlbumUrl);
                if (match.Success && match.Groups.Count > 2)
                {
                    return match.Groups[2].Value.Trim();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
