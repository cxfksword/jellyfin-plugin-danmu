using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiSearchResult
    {
        [JsonPropertyName("data")]
        public IqiyiSearchDoc Data { get; set; }
    }

    public class IqiyiSearchDoc
    {
        [JsonPropertyName("docinfos")]
        public List<IqiyiAlbumDoc> DocInfos { get; set; }
    }

    public class IqiyiAlbumDoc
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("albumDocInfo")]
        public IqiyiSearchAlbumInfo AlbumDocInfo { get; set; }
    }
}
