using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren.Entity;

public class DanmuData
{
    [JsonPropertyName("data")]
    public List<DanmuItem>? Data { get; set; }
}

public class DanmuItem
{
    [JsonPropertyName("p")]
    public string P { get; set; } = string.Empty;

    [JsonPropertyName("d")]
    public string D { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
