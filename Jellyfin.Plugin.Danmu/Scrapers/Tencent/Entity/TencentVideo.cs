using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentVideo
{
    private static readonly Regex regHtml = new Regex(@"<.+?>", RegexOptions.Compiled);

    [JsonIgnore]
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
    [JsonPropertyName("year")]
    public int? Year { get; set; }
    [JsonPropertyName("subjectDoc")]
    public TencentSubjectDoc SubjectDoc { get; set; }
    [JsonPropertyName("videoDoc")]
    public TencentVideoDoc VideoDoc { get; set; }
    [JsonPropertyName("playSites")]
    public List<TencentPlaySite> PlaySites { get; set; }
    [JsonIgnore]
    public List<TencentEpisode> EpisodeList { get; set; }
}

public class TencentSubjectDoc
{
    [JsonPropertyName("videoNum")]
    public int VideoNum { get; set; }
}

public class TencentVideoDoc
{
    [JsonPropertyName("uploader")]
    public string Uploader { get; set; }
}

public class TencentPlaySite
{
    [JsonPropertyName("enName")]
    public string EnName { get; set; }
}
