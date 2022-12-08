using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.ExternalId
{
    /// <inheritdoc />
    public class EpisodeExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => Dandan.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Dandan.ScraperProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

        /// <inheritdoc />
        public string UrlFormatString => "#";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Episode;
    }
}
