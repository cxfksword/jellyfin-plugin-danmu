using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Tencent.ExternalId
{
    public class EpisodeExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => Tencent.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Tencent.ScraperProviderId;

        /// <inheritdoc />
        public string UrlFormatString => "#";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Episode;
    }
}