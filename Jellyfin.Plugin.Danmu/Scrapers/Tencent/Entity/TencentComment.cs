using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
    [JsonPropertyName("content_score")]
    public double ContentScore { get; set; }
    [JsonPropertyName("content_style")]
    public string ContentStyle { get; set; }
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; }
    [JsonPropertyName("show_weight")]
    public int ShowWeight { get; set; }
    [JsonPropertyName("time_offset")]
    public string TimeOffset { get; set; }
    [JsonPropertyName("nick")]
    public string Nick { get; set; }

}

public class TencentCommentContentStyle
{
    [JsonPropertyName("color")]
    public string Color { get; set; }
    [JsonPropertyName("position")]
    public int Position { get; set; }
}