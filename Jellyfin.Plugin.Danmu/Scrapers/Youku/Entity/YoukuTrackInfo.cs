using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuTrackInfo
    {
        [JsonPropertyName("group_id")]
        public string GroupID { get; set; }

        [JsonPropertyName("object_type")]
        public int ObjectType { get; set; }

        [JsonPropertyName("object_title")]
        public string ObjectTitle { get; set; }

        [JsonPropertyName("object_url")]
        public string ObjectUrl { get; set; }

        [JsonIgnore]
        public int? Year { get; set; }

        [JsonIgnore]
        public string Type { get; set; }

    }
}
