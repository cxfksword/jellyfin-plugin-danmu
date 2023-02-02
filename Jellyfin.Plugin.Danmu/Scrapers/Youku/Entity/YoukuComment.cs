using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuComment
    {
        //    "mat": 22,
        // "createtime": 1673844600000,
        // "ver": 1,
        // "propertis": "{\"size\":2,\"color\":16524894,\"pos\":3,\"alpha\":1}",
        // "iid": "XMTM1MTc4MDU3Ng==",
        // "level": 0,
        // "lid": 0,
        // "type": 1,
        // "content": "打自己一拳呗",
        // "extFields": {
        //     "grade": 3,
        //     "voteUp": 0
        // },
        // "ct": 10004,
        // "uid": "UNTYzMTg5NzAzNg==",
        // "uid2": "1407974259",
        // "ouid": "UMTM2OTY2ODYwMA==",
        // "playat": 1320797,
        // "id": 5559881890,
        // "aid": 300707,
        // "status": 99

        [JsonPropertyName("id")]
        public Int64 ID { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        // 毫秒
        [JsonPropertyName("playat")]
        public Int64 Playat { get; set; }

        // "{\"size\":2,\"color\":16524894,\"pos\":3,\"alpha\":1}",
        [JsonPropertyName("propertis")]
        public string Propertis { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; }

    }
}
