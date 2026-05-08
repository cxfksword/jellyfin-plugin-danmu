using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Danmu.ExternalId;

public abstract class BaseExternalId : IExternalId
{
    public string ProviderName => DanmuProviderId.UnifiedProviderName;

    public string Key => DanmuProviderId.UnifiedProviderId;

    public string UrlFormatString => string.Empty;

    public abstract ExternalIdMediaType? Type { get; }

    public abstract bool Supports(IHasProviderIds item);
}

public class MovieExternalId : BaseExternalId
{
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    public override bool Supports(IHasProviderIds item) => item is Movie;
}

public class EpisodeExternalId : BaseExternalId
{
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

    public override bool Supports(IHasProviderIds item) => item is Episode;
}

public class SeasonExternalId : BaseExternalId
{
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Season;

    public override bool Supports(IHasProviderIds item) => item is Season;
}