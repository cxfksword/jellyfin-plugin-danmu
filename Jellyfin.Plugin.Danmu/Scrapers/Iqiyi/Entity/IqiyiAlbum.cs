using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiAlbum
    {
        private static readonly Regex regLinkId = new Regex(@"(v_|a_)(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("albumId")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("name")]
        public String Name { get; set; }

         [JsonPropertyName("firstVideo")]
        public IqiyiVideo FirstVideo { get; set; }

           [JsonPropertyName("latestVideo")]
        public IqiyiVideo LatestVideo { get; set; }
    }

}
