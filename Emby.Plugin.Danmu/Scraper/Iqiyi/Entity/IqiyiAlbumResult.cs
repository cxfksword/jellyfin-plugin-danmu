using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiAlbumResult
    {
        [DataMember(Name="data")]
        public IqiyiAlbum Data { get; set; }
    }
}