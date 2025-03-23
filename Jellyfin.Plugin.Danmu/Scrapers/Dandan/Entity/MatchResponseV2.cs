using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class MatchResponseV2
    {
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("isMatched")]
        public bool IsMatched { get; set; }

        [JsonPropertyName("matches")]
        public List<MatchResultV2> Matches { get; set; }
    }
}
