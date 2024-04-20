using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvCommentSegmentResult
{
    [JsonPropertyName("data")]
    public MgtvCommentSegmentData Data { get; set; }
}


public class MgtvCommentSegmentData
{
    [JsonPropertyName("next")]
    public int Next { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    [JsonPropertyName("items")]
    public List<MgtvComment> Items { get; set; }
}
