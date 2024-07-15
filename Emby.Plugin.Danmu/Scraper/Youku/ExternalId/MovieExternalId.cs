using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Youku.ExternalId
{
    /// <inheritdoc />
    public class MovieExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => Youku.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Youku.ScraperProviderId;

        /// <inheritdoc />
        public string UrlFormatString => "https://v.youku.com/v_show/id_{0}.html";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
