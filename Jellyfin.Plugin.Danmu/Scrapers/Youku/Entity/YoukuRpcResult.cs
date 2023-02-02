using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuRpcResult
    {
        [JsonPropertyName("data")]
        public YoukuRpcData Data { get; set; }
    }

    public class YoukuRpcData
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }
    }
}