using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.ExternalId
{

    /// <inheritdoc />
    public class SeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => Mgtv.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Mgtv.ScraperProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.mgtv.com/h/{0}.html";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Season;
    }

}
