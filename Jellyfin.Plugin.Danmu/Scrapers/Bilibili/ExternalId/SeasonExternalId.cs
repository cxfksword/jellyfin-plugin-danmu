using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.ExternalId
{

    /// <inheritdoc />
    public class SeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => Bilibili.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Bilibili.ScraperProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Season;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.bilibili.com/bangumi/play/ss{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Season;
    }

}
