using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren.Entity;

public class SearchResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<SearchItem> Data { get; set; } = new();
}

public class SearchItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("highlights")]
    public Highlights? Highlights { get; set; }

    [JsonPropertyName("classify")]
    public string Classify { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("cover")]
    public string Cover { get; set; } = string.Empty;
}

public class Highlights
{
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("original_name")]
    public string? OriginalName { get; set; }
}
