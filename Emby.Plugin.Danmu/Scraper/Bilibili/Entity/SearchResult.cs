using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class SearchResult
    {
        [DataMember(Name="result")]
        public List<Media> Result { get; set; }
    }
}