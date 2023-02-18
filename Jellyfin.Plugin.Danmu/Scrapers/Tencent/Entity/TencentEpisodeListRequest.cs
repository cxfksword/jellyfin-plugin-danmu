using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentEpisodeListRequest
{
    [JsonPropertyName("page_params")]
    public TencentPageParams PageParams { get; set; }
}

public class TencentPageParams
{
    [JsonPropertyName("page_type")]
    public string PageType { get; set; } = "detail_operation";
    [JsonPropertyName("page_id")]
    public string PageId { get; set; } = "vsite_episode_list";
    [JsonPropertyName("id_type")]
    public string IdType { get; set; } = "1";
    [JsonPropertyName("page_size")]
    public string PageSize { get; set; } = "100";
    [JsonPropertyName("cid")]
    public string Cid { get; set; }
    [JsonPropertyName("lid")]
    public string Lid { get; set; } = "0";
    [JsonPropertyName("req_from")]
    public string ReqFrom { get; set; } = "web_mobile";
    [JsonPropertyName("page_context")]
    public string PageContext { get; set; } = string.Empty;
}