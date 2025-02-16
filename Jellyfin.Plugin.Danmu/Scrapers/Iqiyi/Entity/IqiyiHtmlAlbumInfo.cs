using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiHtmlAlbumInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("qipuId")]
        public long AlbumId { get; set; }

        [JsonPropertyName("albumName")]
        public string albumName { get; set; }
        
        [JsonPropertyName("videoCount")]
        public int VideoCount { get; set; }
    }
}
