using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.ExternalId
{
    public class MovieExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => Iqiyi.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Iqiyi.ScraperProviderId;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.iqiyi.com/v_{0}.html";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MediaBrowser.Controller.Entities.TV.Episode || item is Movie;
    }
}