using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class Comment
    {
        [DataMember(Name="cid")]
        public long Cid { get; set; }

        // p参数格式为出现时间,模式,颜色,用户ID，各个参数之间使用英文逗号分隔
        [DataMember(Name="p")]
        public string P { get; set; }

        [DataMember(Name="m")]
        public string Text { get; set; }
    }
}