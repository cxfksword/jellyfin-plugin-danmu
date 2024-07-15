using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentComment
    {
        [DataMember(Name="id")]
        public string Id { get; set; }
        [DataMember(Name="content")]
        public string Content { get; set; }
        [DataMember(Name="content_score")]
        public double ContentScore { get; set; }
        [DataMember(Name="content_style")]
        public string ContentStyle { get; set; }
        [DataMember(Name="create_time")]
        public string CreateTime { get; set; }
        [DataMember(Name="show_weight")]
        public int ShowWeight { get; set; }
        [DataMember(Name="time_offset")]
        public string TimeOffset { get; set; }
        [DataMember(Name="nick")]
        public string Nick { get; set; }

    }

    public class TencentCommentContentStyle
    {
        [DataMember(Name="color")]
        public string Color { get; set; }
        [DataMember(Name="position")]
        public int Position { get; set; }
    }
}