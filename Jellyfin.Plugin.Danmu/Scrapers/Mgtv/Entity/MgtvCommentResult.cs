using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvCommentResult
{
    [JsonPropertyName("data")]
    public MgtvCommentData Data { get; set; }
}

public class MgtvCommentData
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
                return string.Empty;
            }

            return CdnList.Split(",").First();
        }
    }
}