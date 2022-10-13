using System.Text.Json.Serialization;

public class DanmuFileInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

}