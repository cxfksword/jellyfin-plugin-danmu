using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Providers
{
    public class MovieProvider// : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        private ILogger<MovieProvider> _logger;
        private BilibiliApi _api;

        public MovieProvider(ILoggerFactory loggerFactory, BilibiliApi api)
        {
            _logger = loggerFactory.CreateLogger<MovieProvider>();
            _api = api;
        }

        public string Name => Plugin.ProviderName;

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>();
            // 检查b站元数据是否为空，是的话，搜索查找匹配的epid
            Console.WriteLine("###################");
            if (string.IsNullOrEmpty(info.Name))
            {
                return result;
            }

            try
            {
                var searchResult = await _api.SearchAsync(info.Name, CancellationToken.None).ConfigureAwait(false);
                if (searchResult.Result != null)
                {
                    foreach (var media in searchResult.Result)
                    {
                        if ((media.ResultType == "media_ft" || media.ResultType == "media_bangumi") && media.Data.Length > 0)
                        {
                            var seasonId = media.Data[0].SeasonId;
                            var season = await _api.GetSeasonAsync(seasonId, CancellationToken.None).ConfigureAwait(false);
                            if (season != null && season.Episodes.Length > 0)
                            {
                                var epId = season.Episodes[0].Id;
                                Console.WriteLine($"###################epId={epId}");

                                // 更新epid元数据
                                result.Item = new Movie
                                {
                                    ProviderIds = new Dictionary<string, string> { { Plugin.ProviderId, $"{epId}" } }
                                };
                                result.HasMetadata = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception handled GetMatchSeasonId");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            if (string.IsNullOrEmpty(searchInfo.Name))
            {
                return list;
            }

            //try
            //{
            //    var searchResult = await _api.SearchAsync(searchInfo.Name, CancellationToken.None).ConfigureAwait(false);
            //    if (searchResult.Result != null)
            //    {
            //        foreach (var media in searchResult.Result)
            //        {
            //            if ((media.ResultType == "media_ft" || media.ResultType == "media_bangumi") && media.Data.Length > 0)
            //            {
            //                foreach (var item in media.Data)
            //                {
            //                    list.Add(new RemoteSearchResult
            //                    {
            //                        ProviderIds = new Dictionary<string, string> { { Plugin.ProviderId, $"{item.SeasonId}" } },
            //                        ImageUrl = item.Cover,
            //                        ProductionYear = Utils.UnixTimeStampToDateTime(item.PublishTime).Year,
            //                        Name = item.Title
            //                    });
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Exception handled GetMatchSeasonId");
            //}

            return list;
        }
    }
}
