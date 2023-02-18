using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentCommentSegmentResult
{
    [JsonPropertyName("barrage_list")]
    public List<TencentComment> BarrageList { get; set; }
}
