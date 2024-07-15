using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Scraper.Tencent.ExternalId
{
    public class MovieExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => Tencent.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Tencent.ScraperProviderId;

        /// <inheritdoc />
        public string UrlFormatString => "https://v.qq.com/x/cover/{0}.html";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}