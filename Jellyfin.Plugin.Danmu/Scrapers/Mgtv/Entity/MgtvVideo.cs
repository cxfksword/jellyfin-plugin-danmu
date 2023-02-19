using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvVideo
{
    private static readonly Regex regHtml = new Regex(@"<.+?>", RegexOptions.Compiled);

    [JsonPropertyName("videoId")]
    public string Id { get; set; }
    [JsonPropertyName("videoType")]
    public int VideoType { get; set; }
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; }
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
    [JsonPropertyName("time")]
    public string Time { get; set; }
    [JsonPropertyName("year")]
    public int? Year { get; set; }
    [JsonIgnore]
    public List<MgtvEpisode> EpisodeList { get; set; }

    public int TotalMinutes
    {
        get
        {
            if (string.IsNullOrEmpty(Time))
            {
                return 0;
            }

            var arr = Time.Split(":");
            if (arr.Length == 2)
            {
                return (int)Math.Ceiling((arr[0].ToDouble() * 60 + arr[1].ToDouble()) / 60);
            }
            if (arr.Length == 3)
            {
                return (int)Math.Ceiling((arr[0].ToDouble() * 3600 + arr[1].ToDouble() * 60 + arr[2].ToDouble()) / 60);
            }

            return 0;
        }
    }
}

