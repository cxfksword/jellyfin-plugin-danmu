using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentSearchDoc
    {
        [DataMember(Name="id")]
        public string Id { get; set; }
        [DataMember(Name="dataType")]
        public int DataType { get; set; }
    }
}