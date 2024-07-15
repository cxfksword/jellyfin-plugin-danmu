using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class VideoEpisode
    {
        [DataMember(Name="id")]
        public long Id { get; set; }

        [DataMember(Name="aid")]
        public long AId { get; set; }

        [DataMember(Name="bvid")]
        public string BvId { get; set; }

        [DataMember(Name="cid")]
        public long CId { get; set; }

        [DataMember(Name="title")]
        public string Title { get; set; }
    }
}