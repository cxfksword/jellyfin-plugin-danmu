using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvEpisodeListResult
{
    [JsonPropertyName("data")]
    public MgtvEpisodeListData Data { get; set; }

}


public class MgtvEpisodeListData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("tab_m")]
    public List<MgtvEpisodeListTab> Tabs { get; set; }
    [JsonPropertyName("pageNo")]
    public int PageNo { get; set; }
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("list")]
    public List<MgtvEpisode> List { get; set; }
}

public class MgtvEpisodeListTab
{
    [JsonPropertyName("m")]
    public string Month { get; set; }
}

