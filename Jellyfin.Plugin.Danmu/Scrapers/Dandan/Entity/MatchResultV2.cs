using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class MatchResultV2
    {
        [JsonPropertyName("episodeId")]
        public long EpisodeId { get; set; }

        [JsonPropertyName("animeId")]
        public long AnimeId { get; set; }

        [JsonPropertyName("animeTitle")]
        public string AnimeTitle { get; set; }

        [JsonPropertyName("episodeTitle")]
        public string EpisodeTitle { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("typeDescription")]
        public string TypeDescription { get; set; }

        [JsonPropertyName("shift")]
        public int Shift { get; set; }
    }
}
