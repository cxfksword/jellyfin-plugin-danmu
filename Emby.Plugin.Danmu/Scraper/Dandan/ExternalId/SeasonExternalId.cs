using Emby.Plugin.Danmu.Core.Singleton;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Dandan.ExternalId
{
    public class SeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => Dandan.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Dandan.ScraperProviderId;

        /// <inheritdoc />
        // public ExternalIdMediaType? Type => null;

        /// <inheritdoc />
        public string UrlFormatString => "https://api.dandanplay.net/api/v2/bangumi/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Season || item is Series;
        }
    }
}