using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiVideoResult
    {
        [DataMember(Name="data")]
        public IqiyiVideo Data { get; set; }
    }
}