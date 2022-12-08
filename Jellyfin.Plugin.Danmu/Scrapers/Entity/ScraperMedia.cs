using System.Collections.Generic;

namespace Jellyfin.Plugin.Danmu.Scrapers.Entity;

public class ScraperMedia
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int? Year { get; set; }

    public List<ScraperEpisode> Episodes { get; set; } = new List<ScraperEpisode>();

}

public class ScraperEpisode
{
    public string Id { get; set; }
    public string CommentId { get; set; }
}
