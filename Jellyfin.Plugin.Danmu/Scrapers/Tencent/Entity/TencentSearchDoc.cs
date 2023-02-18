using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentSearchDoc
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("dataType")]
    public int DataType { get; set; }
}

