using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Danmu.Scrapers.DanmuApi.Entity
{
    public class SearchResponse
    {
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("animes")]
        public List<Anime> Animes { get; set; } = new List<Anime>();
    }

    public class Anime
    {
        private static readonly Regex YearRegex = new Regex(@"\((\d{4})\)", RegexOptions.Compiled);
        private static readonly Regex FromRegex = new Regex(@"from\s+(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [JsonPropertyName("animeId")]
        public long AnimeId { get; set; }

        [JsonPropertyName("bangumiId")]
        public string BangumiId { get; set; } = string.Empty;

        [JsonPropertyName("animeTitle")]
        public string AnimeTitle { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("typeDescription")]
        public string TypeDescription { get; set; } = string.Empty;

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 从 AnimeTitle 中解析年份，格式如：火影忍者疾风传剧场版：羁绊(2008)【电影】from tencent
        /// </summary>
        [JsonIgnore]
        public int? Year
        {
            get
            {
                if (string.IsNullOrEmpty(AnimeTitle))
                {
                    return null;
                }

                var match = YearRegex.Match(AnimeTitle);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (int.TryParse(match.Groups[1].Value, out var year))
                    {
                        // 验证年份在合理范围内（1900-2100）
                        if (year >= 1900 && year <= 2100)
                        {
                            return year;
                        }
                    }
                }

                return null;
            }
        }

    }
}
