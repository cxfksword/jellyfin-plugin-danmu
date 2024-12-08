using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvControlBarrageResult
{
    [JsonPropertyName("data")]
    public MgtvControlBarrage Data { get; set; }
}

public class MgtvControlBarrage
{

    [JsonPropertyName("cdn_list")]
    public string CdnList { get; set; }
    [JsonPropertyName("cdn_version")]
    public string CdnVersion { get; set; }

    public string CdnHost
    {
        get
        {
            if (string.IsNullOrEmpty(CdnList))
            {
                return "bullet-ali.hitv.com";
            }
            return CdnList.Split(',').Where(x => !x.Contains("-ws")).FirstOrDefault("bullet-ali.hitv.com");
        }
    }
}