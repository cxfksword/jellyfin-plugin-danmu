using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class VideoSeason
    {
        [JsonPropertyName("season_id")]
        public long SeasonId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }


        [JsonPropertyName("episodes")]
        public List<VideoEpisode> Episodes { get; set; }

    }
}
