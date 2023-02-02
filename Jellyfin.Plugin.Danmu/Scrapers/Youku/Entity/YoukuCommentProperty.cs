using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuCommentProperty
    {

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("color")]
        public uint Color { get; set; }

        [JsonPropertyName("pos")]
        public int Pos { get; set; }

        [JsonPropertyName("alpha")]
        public int Alpha { get; set; }
    }
}