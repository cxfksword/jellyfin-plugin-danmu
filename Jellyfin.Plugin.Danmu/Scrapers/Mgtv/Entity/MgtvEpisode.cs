using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvEpisode
{
    [JsonPropertyName("src_clip_id")]
    public string SourceClipId { get; set; }
    [JsonPropertyName("clip_id")]
    public string ClipId { get; set; }
    [JsonPropertyName("t1")]
    public string Title { get; set; }
    [JsonPropertyName("t2")]
    public string Title2 { get; set; } = string.Empty;
    [JsonPropertyName("time")]
    public string Time { get; set; }
    [JsonPropertyName("video_id")]
    public string VideoId { get; set; }
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
}