using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class VideoEpisode
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("aid")]
        public long AId { get; set; }

        [JsonPropertyName("bvid")]
        public string BvId { get; set; }

        [JsonPropertyName("cid")]
        public long CId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("badge_type")]
        public int BadgeType { get; set; }
    }
}
