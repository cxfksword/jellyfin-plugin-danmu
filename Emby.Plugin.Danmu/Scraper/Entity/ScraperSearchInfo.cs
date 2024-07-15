namespace Emby.Plugin.Danmu.Scraper.Entity
{
    public class ScraperSearchInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = string.Empty;
        public int? Year { get; set; }
        public int EpisodeSize { get; set; }
    }
}