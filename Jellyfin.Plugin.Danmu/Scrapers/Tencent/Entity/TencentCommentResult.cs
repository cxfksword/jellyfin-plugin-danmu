using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentCommentResult
{
    [JsonPropertyName("segment_span")]
    public string SegmentSpan { get; set; }
    [JsonPropertyName("segment_start")]
    public string SegmentStart { get; set; }

    [JsonPropertyName("segment_index")]
    public Dictionary<long, TencentCommentSegment> SegmentIndex { get; set; }
}

public class TencentCommentSegment
{
    [JsonPropertyName("segment_name")]
    public string SegmentName { get; set; }
    [JsonPropertyName("segment_start")]
    public string SegmentStart { get; set; }
}