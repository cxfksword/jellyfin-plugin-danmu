using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Api.Entity
{
    public class VideoSeason
    {
        [JsonPropertyName("season_id")]
        public long SeasonId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }


        [JsonPropertyName("episodes")]
        public VideoEpisode[] Episodes { get; set; }

    }
}
