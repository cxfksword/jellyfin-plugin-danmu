using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiSearchResult
    {
        [DataMember(Name = "data")] public IqiyiSearchDoc Data { get; set; }
    }

    public class IqiyiSearchDoc
    {
        [DataMember(Name = "docinfos")] public List<IqiyiAlbumDoc> DocInfos { get; set; }
    }

    public class IqiyiAlbumDoc
    {
        [DataMember(Name = "score")] public double Score { get; set; }

        [DataMember(Name = "albumDocInfo")] public IqiyiSearchAlbumInfo AlbumDocInfo { get; set; }
    }
}