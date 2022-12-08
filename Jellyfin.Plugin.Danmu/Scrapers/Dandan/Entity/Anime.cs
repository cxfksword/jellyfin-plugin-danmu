using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class Anime
    {

        [JsonPropertyName("animeId")]
        public long AnimeId { get; set; }

        [JsonPropertyName("animeTitle")]
        public string AnimeTitle { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("typeDescription")]
        public string TypeDescription { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("episodeCount")]
        public int? EpisodeCount { get; set; }

        [JsonPropertyName("episodes")]
        public List<Episode>? Episodes { get; set; }

        public int? Year
        {
            get
            {
                try
                {
                    if (StartDate == null)
                    {
                        return null;
                    }

                    return DateTime.Parse(StartDate).Year;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
