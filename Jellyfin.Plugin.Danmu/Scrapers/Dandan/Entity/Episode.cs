using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class Episode
    {
        [JsonPropertyName("episodeId")]
        public long EpisodeId { get; set; }

        [JsonPropertyName("episodeTitle")]
        public string EpisodeTitle { get; set; }

        [JsonPropertyName("episodeNumber")]
        public string EpisodeNumber { get; set; }
    }
}