using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentEpisode
    {
        [DataMember(Name="vid")]
        public string Vid { get; set; }
        [DataMember(Name="cid")]
        public string Cid { get; set; }
        [DataMember(Name="duration")]
        public string Duration { get; set; }
        [DataMember(Name="title")]
        public string Title { get; set; }
        [DataMember(Name="is_trailer")]
        public string IsTrailer { get; set; }
    }
}