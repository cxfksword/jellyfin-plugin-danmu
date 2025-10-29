using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.ExternalId
{
    /// <inheritdoc />
    public class MovieExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => Dandan.ScraperProviderName;

        /// <inheritdoc />
        public string Key => Dandan.ScraperProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null;
        
        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
