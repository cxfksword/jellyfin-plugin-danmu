using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Dandan.ExternalId
{
    public class MovieExternalId:IExternalId
    {
        /// <inheritdoc />
        public string Name => Dandan.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Dandan.ScraperProviderId;

        // /// <inheritdoc />
        // public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

        /// <inheritdoc />
        public string UrlFormatString => "#";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Movie || item is MediaBrowser.Controller.Entities.TV.Episode;
    }
}