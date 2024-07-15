using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class VideoPart
    {
        [DataMember(Name="cid")]
        public long Cid { get; set; }
        [DataMember(Name="page")]
        public int Page { get; set; }
    }
}