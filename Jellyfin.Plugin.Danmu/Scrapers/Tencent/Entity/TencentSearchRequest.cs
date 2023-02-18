using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentSearchRequest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    [JsonPropertyName("filterValue")]
    public string FilterValue { get; set; } = "firstTabid=150";
    [JsonPropertyName("retry")]
    public int Retry { get; set; } = 0;
    [JsonPropertyName("query")]
    public string Query { get; set; }
    [JsonPropertyName("pagenum")]
    public int PageNum { get; set; } = 0;
    [JsonPropertyName("pagesize")]
    public int PageSize { get; set; } = 20;
    [JsonPropertyName("queryFrom")]
    public int QueryFrom { get; set; } = 4;
    [JsonPropertyName("isneedQc")]
    public bool IsneedQc { get; set; } = true;
    [JsonPropertyName("adRequestInfo")]
    public string AdRequestInfo { get; set; } = string.Empty;
    [JsonPropertyName("sdkRequestInfo")]
    public string SdkRequestInfo { get; set; } = string.Empty;
    [JsonPropertyName("sceneId")]
    public int SceneId { get; set; } = 21;
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "23";
}
