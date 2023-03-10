using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiHtmlAlbumInfo
    {
        [JsonPropertyName("qipuId")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("albumName")]
        public string AlbumName { get; set; }
        [JsonPropertyName("albumPageUrl")]
        public string AlbumUrl { get; set; }
        [JsonPropertyName("videoCount")]
        public int VideoCount { get; set; }


    }
}
