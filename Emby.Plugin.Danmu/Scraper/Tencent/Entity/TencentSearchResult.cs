using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentSearchResult
    {
        [DataMember(Name="data")]
        public TencentSearchData Data { get; set; }
    }

    public class TencentSearchData
    {
        [DataMember(Name="normalList")]
        public TencentSearchBox NormalList { get; set; }
    }

    public class TencentSearchBox
    {
        [DataMember(Name="itemList")]
        public List<TencentSearchItem> ItemList { get; set; }
    }

    public class TencentSearchItem
    {
        [DataMember(Name="doc")]
        public TencentSearchDoc Doc { get; set; }
        [DataMember(Name="videoInfo")]
        public TencentVideo VideoInfo { get; set; }

    }
}