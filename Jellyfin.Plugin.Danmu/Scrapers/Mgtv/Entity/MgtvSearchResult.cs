using System;
using System.Collections.Generic;
using System.Linq;
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
    public MgtvSearchItem Data { get; set; }
}

public class MgtvSearchItem
{
    private static readonly Regex regHtml = new Regex(@"<.+?>", RegexOptions.Compiled);
    private static readonly Regex regId = new Regex(@"\/(?:b|h)\/(\d+)", RegexOptions.Compiled);
    private static readonly Regex regYear = new Regex(@"year=(\d{4})", RegexOptions.Compiled);

    private string _title = string.Empty;

    /// <summary>
    /// 标题（电视剧/电影使用title字段，综艺分季使用hitTitle字段，会去除html标签）。
    /// </summary>
    [JsonPropertyName("title")]
    public string Title
    {
        get
        {
            return regHtml.Replace(_title, "");
        }
        set
        {
            _title = value ?? string.Empty;
        }
    }

    [JsonPropertyName("hitTitle")]
    public string HitTitle { get; set; }

    [JsonPropertyName("year")]
    public string YearRaw { get; set; }

    [JsonPropertyName("desc")]
    public List<MgtvSearchDesc> Desc { get; set; }

    [JsonPropertyName("sourceList")]
    public List<MgtvSearchSource> SourceList { get; set; }

    /// <summary>
    /// 综艺类节目的分季列表，例如：大侦探 第十一季。
    /// </summary>
    [JsonPropertyName("yearList")]
    public List<MgtvSearchItem> YearList { get; set; }

    /// <summary>
    /// 芒果TV（imgo）的播放地址。
    /// </summary>
    public string Url
    {
        get
        {
            var imgoUrl = SourceList?.FirstOrDefault(x => x.Source == "imgo" && !string.IsNullOrEmpty(x.Url))?.Url;
            if (!string.IsNullOrEmpty(imgoUrl))
            {
                return imgoUrl;
            }

            return SourceList?.FirstOrDefault(x => !string.IsNullOrEmpty(x.Url))?.Url ?? string.Empty;
        }
    }

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
            var typeDesc = Desc?.FirstOrDefault(x => string.Equals(x.Label, "类型", StringComparison.Ordinal));
            if (typeDesc == null || string.IsNullOrEmpty(typeDesc.Text))
            {
                return string.Empty;
            }

            return typeDesc.Text;
        }
    }

    public int? Year
    {
        get
        {
            if (int.TryParse(YearRaw, out var year))
            {
                return year;
            }

            var moreUrl = SourceList?.FirstOrDefault(x => !string.IsNullOrEmpty(x.MoreUrl))?.MoreUrl;
            if (!string.IsNullOrEmpty(moreUrl))
            {
                var match = regYear.Match(moreUrl);
                if (match.Success)
                {
                    return match.Groups[1].Value.ToInt();
                }
            }

            return null;
        }
    }

    public int VideoCount
    {
        get
        {
            var videoList = SourceList?.FirstOrDefault(x => x.VideoList != null && x.VideoList.Count > 0)?.VideoList;
            return videoList?.Count ?? 0;
        }
    }
}

public class MgtvSearchDesc
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class MgtvSearchSource
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("moreUrl")]
    public string MoreUrl { get; set; }

    [JsonPropertyName("vid")]
    public string Vid { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("videoList")]
    public List<MgtvSearchVideo> VideoList { get; set; }
}

public class MgtvSearchVideo
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("vid")]
    public string Vid { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}
