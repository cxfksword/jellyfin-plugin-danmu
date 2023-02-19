using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvSearchResult
{
    [JsonPropertyName("data")]
    public MgtvSearchData Data { get; set; }
}

public class MgtvSearchData
{
    [JsonPropertyName("contents")]
    public List<MgtvSearchContent> Contents { get; set; }
}

public class MgtvSearchContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("data")]
    public List<MgtvSearchItem> Data { get; set; }

}

public class MgtvSearchItem
{
    private static readonly Regex regHtml = new Regex(@"<.+?>", RegexOptions.Compiled);
    private static readonly Regex regId = new Regex(@"\/b\/(\d+)\/(\d+)", RegexOptions.Compiled);

    private static readonly Regex regYear = new Regex(@"[12][890][0-9][0-9]", RegexOptions.Compiled);

    [JsonPropertyName("jumpKind")]
    public string JumpKind { get; set; }
    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    private string _title = string.Empty;
    [JsonPropertyName("title")]
    public string Title
    {
        get
        {
            return regHtml.Replace(_title, "");
        }
        set
        {
            _title = value;
        }
    }

    [JsonPropertyName("url")]
    public string Url { get; set; }


    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(Url))
            {
                return string.Empty;
            }

            var match = regId.Match(Url);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }

    public string TypeName
    {
        get
        {
            if (Desc == null || Desc.Count <= 0)
            {
                return string.Empty;
            }

            return Desc.First().Split("/").First().Replace("类型:", "").Trim();
        }
    }

    public int? Year
    {
        get
        {
            if (Desc == null || Desc.Count <= 0)
            {
                return null;
            }

            var match = regYear.Match(Desc.First());
            if (match.Success)
            {
                return match.Value.ToInt();
            }

            return null;
        }
    }


}


