using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity
{
    public class YoukuCommentResult
    {
        [JsonPropertyName("data")]
        public YoukuCommentData Data { get; set; }
    }

    public class YoukuCommentData
    {
        [JsonPropertyName("result")]
        public List<YoukuComment> Result { get; set; }
    }

}