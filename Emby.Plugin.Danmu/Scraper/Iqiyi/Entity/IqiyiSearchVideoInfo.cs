using System;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiSearchVideoInfo
    {
        [DataMember(Name="tvId")]
        public Int64 VideoId { get; set; }

        [DataMember(Name="itemLink")]
        public string ItemLink { get; set; }
    }
}