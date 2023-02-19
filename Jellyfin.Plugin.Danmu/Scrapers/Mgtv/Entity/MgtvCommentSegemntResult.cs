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
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<MgtvComment> Items { get; set; }
}
