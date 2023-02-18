using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentEpisode
{
    [JsonPropertyName("vid")]
    public string Vid { get; set; }
    [JsonPropertyName("cid")]
    public string Cid { get; set; }
    [JsonPropertyName("duration")]
    public string Duration { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("is_trailer")]
    public string IsTrailer { get; set; }
}