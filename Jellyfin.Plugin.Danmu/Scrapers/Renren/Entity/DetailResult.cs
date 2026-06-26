using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren.Entity;

public class DetailResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public DetailData? Data { get; set; }
}

public class DetailData
{
    [JsonPropertyName("episodeList")]
    public List<EpisodeItem>? EpisodeList { get; set; }

    [JsonPropertyName("dramaInfo")]
    public DramaInfo? DramaInfo { get; set; }

    [JsonPropertyName("watchInfo")]
    public WatchInfo? WatchInfo { get; set; }
}

public class EpisodeItem
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("episodeNo")]
    public int EpisodeNo { get; set; }
}

public class DramaInfo
{
    [JsonPropertyName("enName")]
    public string? EnName { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("dramaType")]
    public string? DramaType { get; set; }

    [JsonPropertyName("plotType")]
    public string? PlotType { get; set; }
}

public class WatchInfo
{
    [JsonPropertyName("m3u8")]
    public PlayInfo? M3U8 { get; set; }

    [JsonPropertyName("tria4kPlayInfo")]
    public PlayInfo? Tria4kPlayInfo { get; set; }
}

public class PlayInfo
{
    [JsonPropertyName("startingLength")]
    public int StartingLength { get; set; }

    [JsonPropertyName("openingLength")]
    public int OpeningLength { get; set; }
}
