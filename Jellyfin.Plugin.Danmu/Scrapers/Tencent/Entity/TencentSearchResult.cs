using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentSearchResult
{
    [JsonPropertyName("data")]
    public TencentSearchData Data { get; set; }
}

public class TencentSearchData
{
    [JsonPropertyName("normalList")]
    public TencentSearchBox NormalList { get; set; }
}

public class TencentSearchBox
{
    [JsonPropertyName("itemList")]
    public List<TencentSearchItem> ItemList { get; set; }
}

public class TencentSearchItem
{
    [JsonPropertyName("doc")]
    public TencentSearchDoc Doc { get; set; }
    [JsonPropertyName("videoInfo")]
    public TencentVideo VideoInfo { get; set; }

}


