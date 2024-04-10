using System.Text.Json.Serialization;

public class MediaInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("episode_size")]
    public int EpisodeSize { get; set; } = 0;

    [JsonPropertyName("site")]
    public string Site { get; set; } = string.Empty;


    [JsonPropertyName("site_id")]
    public string SiteId { get; set; } = string.Empty;
}