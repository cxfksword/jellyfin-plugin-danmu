using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvVideoInfoResult
{
    [JsonPropertyName("data")]
    public MgtvVideoInfoData Data { get; set; }

}

public class MgtvVideoInfoData
{
    [JsonPropertyName("info")]
    public MgtvVideo Info { get; set; }
}