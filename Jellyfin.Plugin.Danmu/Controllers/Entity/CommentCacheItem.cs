
using Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity;

namespace Jellyfin.Plugin.Danmu.Controllers.Entity
{
    public class CommentCacheItem
    {

        public string ScraperProviderId { get; set; } = string.Empty;

        public string CommentId { get; set; }

        public CommentCacheItem(string providerId, string commentId)
        {
            ScraperProviderId = providerId;
            CommentId = commentId;
        }
    }
}