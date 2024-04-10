using System.Text.Json.Serialization;

public class EpisodeInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("cid")]
    public string CommentId { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; } = 0;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}