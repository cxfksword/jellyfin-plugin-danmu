using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Danmu.Configuration;

/// <summary>
/// The configuration options.
/// </summary>
public enum SomeOptions
{
    /// <summary>
    /// Option one.
    /// </summary>
    OneOption,

    /// <summary>
    /// Second option.
    /// </summary>
    AnotherOption
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ToAss = false;
        AssFont = string.Empty;
        AssFontSize = string.Empty;
        AssLineCount = string.Empty;
        AssSpeed = string.Empty;
        AssTextOpacity = string.Empty;

    }

    /// <summary>
    /// 是否同时生成ASS格式弹幕.
    /// </summary>
    public bool ToAss { get; set; }

    /// <summary>
    /// 字体.
    /// </summary>
    public string AssFont { get; set; }

    /// <summary>
    /// 字体大小.
    /// </summary>
    public string AssFontSize { get; set; }

    /// <summary>
    /// 限制行数.
    /// </summary>
    public string AssLineCount { get; set; }

    /// <summary>
    /// 移动速度.
    /// </summary>
    public string AssSpeed { get; set; }

    /// <summary>
    /// 透明度.
    /// </summary>
    public string AssTextOpacity { get; set; }

}
