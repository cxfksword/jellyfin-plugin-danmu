using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuEpisode
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("seq")]
        public string Seq { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("rc_title")]
        public string RCTitle { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }


        public int TotalMat
        {
            get
            {
                var duration = Duration.ToDouble();
                return (int)Math.Floor(duration / 60) + 1;
            }

        }
    }
}
