using System.Collections.Generic;

namespace Jellyfin.Plugin.Danmu.Scrapers.Entity;

public class ScraperSearchInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int EpisodeSize { get; set; }

    public string StartDate {
        get
        {
            if (Year.HasValue)
            {
                return $"{Year}-01-01T00:00:00Z";
            }
            else
            {
                return "1970-01-01T00:00:00Z";
            }
        }
    }
}