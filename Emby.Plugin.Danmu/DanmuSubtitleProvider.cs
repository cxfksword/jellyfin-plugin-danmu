using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Model;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;

namespace Emby.Plugin.Danmu
{
    public class DanmuSubtitleProvider : ISubtitleProvider
    {
        public string Name => "Danmu";
        // public int Order => 1;

        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;

        private readonly ScraperManager _scraperManager;

        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType>()
            { VideoContentType.Movie, VideoContentType.Episode };

        public DanmuSubtitleProvider(ILibraryManager libraryManager, ILogManager logManager,
            LibraryManagerEventsHelper libraryManagerEventsHelper)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(GetType().ToString());
            _scraperManager = SingletonManager.ScraperManager;
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            _logger.Info("开始查询弹幕 id={0}", id);
            var base64EncodedBytes = System.Convert.FromBase64String(id);
            id = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            var info = id.FromJson<SubtitleId>();
            if (info == null)
            {
                throw new ArgumentException();
            }

            var item = _libraryManager.GetItemById(info.ItemId);
            if (item == null)
            {
                throw new ArgumentException();
            }

            var scraper = _scraperManager.All().FirstOrDefault(x => x.ProviderId == info.ProviderId);
            if (scraper != null)
            {
                // 注意！！：item这里要使用临时对象，假如直接修改原始item的ProviderIds，会导致直接修改原始item数据
                if (item is Movie)
                {
                    item = new Movie()
                    {
                        Id = item.Id, Name = item.Name,
                        ProviderIds = new ProviderIdDictionary() { { scraper.ProviderId, info.Id } }
                    };
                }

                if (item is Episode)
                {
                    item = new Episode()
                    {
                        Id = item.Id, Name = item.Name,
                        ProviderIds = new ProviderIdDictionary() { { scraper.ProviderId, info.Id } }
                    };
                }

                _libraryManagerEventsHelper.QueueItem(item, EventType.Force);
            }

            throw new CanIgnoreException($"弹幕下载已由{Plugin.Instance?.Name}插件接管.");
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            _logger.Info("开始查询弹幕 request={0}", request);
            var list = new List<RemoteSubtitleInfo>();
            bool isForced = request.IsForced != null && (bool)request.IsForced;
            if (isForced || string.IsNullOrEmpty(request.MediaPath))
            {
                return list;
            }

            var item = _libraryManager.GetItemList(new InternalItemsQuery
            {
                Path = request.MediaPath,
            }).FirstOrDefault();

            if (item == null)
            {
                return list;
            }

            // 媒体库未启用就不处理
            if (_libraryManagerEventsHelper.IsIgnoreItem(item))
            {
                return list;
            }

            // 剧集使用series名称进行搜索
            if (item is Episode)
            {
                item.Name = request.SeriesName;
            }

            foreach (var scraper in _scraperManager.All())
            {
                try
                {
                    var result = await scraper.Search(item);
                    foreach (var searchInfo in result)
                    {
                        var title = searchInfo.Name;
                        if (!string.IsNullOrEmpty(searchInfo.Category))
                        {
                            title = $"[{searchInfo.Category}] {searchInfo.Name}";
                        }

                        if (searchInfo.Year != null && searchInfo.Year > 0)
                        {
                            title += $" ({searchInfo.Year})";
                        }

                        if (item is Episode && searchInfo.EpisodeSize > 0)
                        {
                            title += $"【共{searchInfo.EpisodeSize}集】";
                        }

                        var idInfo = new SubtitleId()
                        {
                            ItemId = item.Id.ToString(), Id = searchInfo.Id.ToString(), ProviderId = scraper.ProviderId
                        };
                        list.Add(new RemoteSubtitleInfo()
                        {
                            Id = idInfo.ToJson().ToBase64(), // Id不允许特殊字幕，做base64编码处理
                            Name = title,
                            ProviderName = $"{Name}",
                            Format = "xml",
                            Comment = $"来源：{scraper.Name}",
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{0}]Exception handled processing queued movie events", scraper.Name);
                }
            }


            return list;
        }

        private void UpdateDanmuMetadata(BaseItem item, string providerId, string providerVal)
        {
            // 先清空旧弹幕的所有元数据
            foreach (var s in _scraperManager.All())
            {
                item.ProviderIds.Remove(s.ProviderId);
            }

            // 保存指定弹幕元数据
            item.ProviderIds[providerId] = providerVal;
        }
    }
}