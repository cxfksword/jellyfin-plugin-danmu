using System.Collections.Generic;

namespace Jellyfin.Plugin.Danmu.Scrapers.Entity;

public class ScraperSearchInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? Year { get; set; }
}