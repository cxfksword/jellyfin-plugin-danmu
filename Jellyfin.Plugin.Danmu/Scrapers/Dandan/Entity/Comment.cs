using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity
{
    public class Comment
    {
        [JsonPropertyName("cid")]
        public long Cid { get; set; }

        // p参数格式为出现时间,模式,颜色,用户ID，各个参数之间使用英文逗号分隔
        [JsonPropertyName("p")]
        public string P { get; set; }

        [JsonPropertyName("m")]
        public string Text { get; set; }

        [JsonPropertyName("t")]
        public uint Time { get; set; }
    }
}
