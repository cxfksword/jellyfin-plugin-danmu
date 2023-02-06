using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class VideoUgcSeason
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("sections")]
        public List<VideoUgcSection> Sections { get; set; }
    }

    public class VideoUgcSection
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("episodes")]
        public List<VideoEpisode> Episodes { get; set; }
    }
}
