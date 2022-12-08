using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class VideoPart
    {
        [JsonPropertyName("cid")]
        public long Cid { get; set; }
        [JsonPropertyName("page")]
        public int Page { get; set; }
    }
}
