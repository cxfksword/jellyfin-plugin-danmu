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
        [JsonPropertyName("episodes")]
        public List<VideoEpisode> Episodes { get; set; }

    }
}
