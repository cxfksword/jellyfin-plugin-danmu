using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentSearchRequest
{
    [JsonPropertyName("filterValue")]
    public string FilterValue { get; set; } = "";
    [JsonPropertyName("retry")]
    public int Retry { get; set; } = 0;
    [JsonPropertyName("query")]
    public string Query { get; set; }
    [JsonPropertyName("pagenum")]
    public int PageNum { get; set; } = 0;
    [JsonPropertyName("pagesize")]
    public int PageSize { get; set; } = 20;
    [JsonPropertyName("adRequestInfo")]
    public string AdRequestInfo { get; set; } = string.Empty;
    [JsonPropertyName("sdkRequestInfo")]
    public string SdkRequestInfo { get; set; } = string.Empty;
}
