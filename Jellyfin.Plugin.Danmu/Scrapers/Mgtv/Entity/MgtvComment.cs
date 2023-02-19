using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;

public class MgtvComment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("ids")]
    public string Ids { get; set; }
    [JsonPropertyName("type")]
    public int Type { get; set; }
    [JsonPropertyName("uid")]
    public long Uid { get; set; }
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
    [JsonPropertyName("time")]
    public int Time { get; set; }
    [JsonPropertyName("v2_color")]
    public MgtvCommentColor Color { get; set; }

}

public class MgtvCommentColor
{
    [JsonPropertyName("color_left")]
    public MgtvCommentColorRGB ColorLeft { get; set; }
    [JsonPropertyName("color_right")]
    public MgtvCommentColorRGB ColorRight { get; set; }
}


public class MgtvCommentColorRGB
{
    [JsonPropertyName("r")]
    public int R { get; set; }
    [JsonPropertyName("g")]
    public int G { get; set; }
    [JsonPropertyName("b")]
    public int B { get; set; }

    public uint HexNumber
    {
        get
        {
            return (uint)((R << 16) | (G << 8) | (B));
        }
    }
}
