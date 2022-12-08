using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class AnimeResult
    {
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }


        [JsonPropertyName("bangumi")]
        public Anime? Bangumi { get; set; }
    }
}
