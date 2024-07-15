using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class BiliplusVideo
    {
        
        [DataMember(Name="aid")]
        public long AId { get; set; }

        [DataMember(Name="title")]
        public string Title { get; set; }

        [DataMember(Name="list")]
        public VideoPart[] List { get; set; }
    }
}