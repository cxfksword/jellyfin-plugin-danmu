using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuSearchResult
    {
        [JsonPropertyName("pageComponentList")]
        public List<YoukuSearchComponent> PageComponentList { get; set; }
    }

    public class YoukuSearchComponent
    {
        [JsonPropertyName("commonData")]
        public YoukuSearchComponentData CommonData { get; set; }
    }

    public class YoukuSearchComponentData
    {
        [JsonPropertyName("showId")]
        public string ShowId { get; set; }

        [JsonPropertyName("episodeTotal")]
        public int EpisodeTotal { get; set; }

        [JsonPropertyName("feature")]
        public string Feature { get; set; }

        [JsonPropertyName("isYouku")]
        public int IsYouku { get; set; }

        [JsonPropertyName("hasYouku")]
        public int HasYouku { get; set; }

        [JsonPropertyName("ugcSupply")]
        public int UgcSupply { get; set; }

        [JsonPropertyName("titleDTO")]
        public YoukuSearchTitle TitleDTO { get; set; }
    }

    public class YoukuSearchTitle
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
    }
}