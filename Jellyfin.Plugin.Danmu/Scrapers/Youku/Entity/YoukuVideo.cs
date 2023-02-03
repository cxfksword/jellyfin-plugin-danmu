using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuVideo
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("videos")]
        public List<YoukuEpisode> Videos { get; set; } = new List<YoukuEpisode>();

        [JsonIgnore]
        public string ID { get; set; }

        [JsonIgnore]
        public string Title { get; set; }

        [JsonIgnore]
        public int? Year { get; set; }

        [JsonIgnore]
        public string Type { get; set; }

    }
}
