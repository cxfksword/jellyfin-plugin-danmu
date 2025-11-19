namespace Jellyfin.Plugin.Danmu.Controllers.Entity
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// 表示由弹幕源抓取并缓存的番剧条目。
    /// </summary>
    public class AnimeCacheItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnimeCacheItem"/> class，用于封装弹幕源缓存的番剧条目.
        /// </summary>
        /// <param name="scraperProviderId">弹幕源提供的唯一 ProviderId。</param>
        /// <param name="id">对应弹幕源的番剧标识。</param>
        /// <param name="animeData">弹幕源返回的番剧原始数据。</param>
        [JsonConstructor]
        public AnimeCacheItem(string scraperProviderId, string id, Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Anime? animeData)
        {
            this.ScraperProviderId = scraperProviderId;
            this.Id = id;
            this.AnimeData = animeData;
        }

        /// <summary>
        /// Gets or sets 弹幕源 ProviderId（例如 BilibiliID）.
        /// </summary>
        public string ScraperProviderId { get; set; }

        /// <summary>
        /// Gets or sets 弹幕源内部使用的番剧编号.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets 弹幕源返回的番剧原始数据对象.
        /// </summary>
        public Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Anime? AnimeData { get; set; }
    }
}