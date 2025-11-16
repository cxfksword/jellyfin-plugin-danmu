namespace Jellyfin.Plugin.Danmu.Controllers.Entity
{
    public class AnimeCacheItem
    {

        public string ScraperProviderId { get; set; }

        public string Id { get; set; }

        public Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Anime? AnimeData { get; set; }

        public AnimeCacheItem(string providerId, string id, Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Anime anime)
        {
            ScraperProviderId = providerId;
            Id = id;
            AnimeData = anime;
        }
    }
}