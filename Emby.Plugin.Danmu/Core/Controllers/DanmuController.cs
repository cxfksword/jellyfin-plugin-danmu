using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Emby.Plugin.Danmu.Core.Controllers.Dto;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Api;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;

namespace Emby.Plugin.Danmu.Core.Controllers
{
    [Route("/plugin/danmu/{id}")]
    [Route("/api/danmu/{id}")]
    [Route("/api/danmu/search")]
    public class DanmuParams : IReturn<object>
    {
        [DataMember(Name="id")]
        public string Id { get; set; } = string.Empty;
        
        [DataMember(Name="needSites")]
        public List<string> NeedSites { get; set; } = new List<string>();
        
        [DataMember(Name="option")]
        public string Option { get; set; } = DanmuDispatchOption.GetJsonById;
        
        [DataMember(Name="keyword")]
        public string Keyword { get; set; } = string.Empty;
    }

    public class DanmuController : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;
        private readonly MediaBrowser.Model.IO.IFileSystem _fileSystem;
        private readonly ScraperManager _scraperManager;
        
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DanmuController"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logManager"></param>
        public DanmuController(
            MediaBrowser.Model.IO.IFileSystem fileSystem,
            ILogManager logManager,
            LibraryManagerEventsHelper libraryManagerEventsHelper,
            ILibraryManager libraryManager)
        {
            
            _fileSystem = fileSystem;
            _logger = logManager.getDefaultLogger();
            _libraryManager = libraryManager;
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
            _scraperManager = SingletonManager.ScraperManager;
            _jsonSerializer = SingletonManager.JsonSerializer;
        }

        /// <summary>
        /// 获取弹幕文件内容.
        /// </summary>
        /// <returns>xml弹幕文件内容</returns>
        public async Task<object> Any(DanmuParams danmuParams)
        {
            _logger.Info("当前请求信息 danmuParams={0}", danmuParams.ToJson());
            
            // 获取json格式弹幕
            if (DanmuDispatchOption.GetJsonById.Equals(danmuParams.Option))
            {
                return await GetDanmuForJson(danmuParams).ConfigureAwait(false);
            }

            if (DanmuDispatchOption.Refresh.Equals(danmuParams.Option))
            {
                return await Refresh(danmuParams.Id);
            }

            if (DanmuDispatchOption.SearchDanmu.Equals(danmuParams.Option))
            {
                return await SearchDanmu(danmuParams.Keyword); 
            }

            // 获取支持的站点弹幕信息
            if (DanmuDispatchOption.GetAllSupportSite.Equals(danmuParams.Option))
            {
                DanmuResultDto result = new DanmuResultDto();
                var allWithNoEnabled = _scraperManager.AllWithNoEnabled();
                List<DanmuSourceDto> sources = new List<DanmuSourceDto>(allWithNoEnabled.Count);
                foreach (AbstractScraper scraper in allWithNoEnabled)
                {
                    DanmuSourceDto source = new DanmuSourceDto();
                    source.Source = scraper.ProviderId;
                    source.SourceName = scraper.ProviderName;
                    source.Opened = scraper.DefaultEnable;
                    sources.Add(source);
                }
                result.Data = sources;
                return result;
            }

            return "暂不支持的操作: " + danmuParams.Option;
        }

        private async Task<DanmuResultDto> GetDanmuForJson(DanmuParams danmuParams)
        {
            var currentItem = _libraryManager.GetItemById(danmuParams.Id);
            if (currentItem == null)
            {
                return new DanmuResultDto();
            }

            List<string> sites = danmuParams.NeedSites;
            DanmuResultDto danmuResultDto = new DanmuResultDto();
            if (sites == null || sites.Count == 0)
            {
                var count = _scraperManager.All().Count;
                if (count == 0)
                {
                    return danmuResultDto;
                }

                sites = _scraperManager
                    .All()
                    .Select(s => s.ProviderId)
                    .ToList();
            }
            
            List<DanmuSourceDto> danmuSources= new List<DanmuSourceDto>(sites.Count);
            List<Task<DanmuSourceDto>> danmuSourceTasks= new List<Task<DanmuSourceDto>>(sites.Count);
            danmuResultDto.Data = danmuSources;
            
            foreach (string site in sites)
            {
                if (site == null)
                {
                    continue;
                }
                
                Task<DanmuSourceDto> danmuSourceTask = GetDanmuSourceDto(currentItem, site);
                danmuSourceTasks.Add(danmuSourceTask);
            }
            
            await Task.WhenAll(danmuSourceTasks).ConfigureAwait(false);
            foreach(Task<DanmuSourceDto> danmuSourceTask in danmuSourceTasks) 
            {
                var danmuSourceDto = danmuSourceTask.GetAwaiter().GetResult();
                if (danmuSourceDto != null && danmuSourceDto.Source != null)
                {
                    danmuSources.Add(danmuSourceDto);       
                }
            }
            
            _logger.Info("任务添加完成 准备输出 danmuResultDto={0}", danmuResultDto.ToJson());
            return danmuResultDto;
        }

        private Task<DanmuSourceDto> GetDanmuSourceDto(BaseItem currentItem, string site)
        {
            var danmuPath = Path.Combine(currentItem.ContainingFolderPath, currentItem.FileNameWithoutExtension + "_" + site + ".xml");
            _logger.Info("弹幕文件路径 danmuPath={0}", danmuPath);
            var fileMeta = _fileSystem.GetFileInfo(danmuPath);
            if (!fileMeta.Exists)
            {
                return Task.FromResult<DanmuSourceDto>(null);
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(danmuPath);
            XmlElement xmlNode = xmlDocument.DocumentElement;
            if (xmlNode == null)
            {
                return Task.FromResult<DanmuSourceDto>(null);
            }
            DanmuSourceDto danmuSourceDto = new DanmuSourceDto();
            List<DanmuEventDTO> danmuEventDtos = new List<DanmuEventDTO>();
            foreach (XmlNode node in xmlNode.ChildNodes) //4.遍历根节点（根节点包含所有节点）
            {
                // _logger.Info("XmlNode.InnerText={0}", node.InnerText);
                if ("sourceprovider".Equals(node.Name))
                {
                    danmuSourceDto.Source = node.InnerText;
                }
                else if ("datasize".Equals(node.Name) && danmuEventDtos.Count == 0)
                {
                    danmuEventDtos = new List<DanmuEventDTO>(int.Parse(node.InnerText));
                }
                else if ("d".Equals(node.Name) && node is XmlElement)
                {
                    DanmuEventDTO danmuEvent = new DanmuEventDTO();
                    danmuEvent.M = node.InnerText;
                    danmuEvent.P = ((XmlElement)node).GetAttribute("p");
                    danmuEventDtos.Add(danmuEvent);
                }
            }

            if (danmuSourceDto.Source == null)
            {
                return Task.FromResult<DanmuSourceDto>(null);
            }

            danmuSourceDto.DanmuEvents = danmuEventDtos;
            return Task.FromResult(danmuSourceDto);
        }

        /// <summary>
        /// 获取弹幕文件内容.
        /// </summary>
        /// <returns>xml弹幕文件内容</returns>
        // public async Task<ActionResult> Download(string id)
        // {
        //     if (string.IsNullOrEmpty(id))
        //     {
        //         throw new ResourceNotFoundException();
        //     }
        //
        //     var currentItem = _libraryManager.GetItemById(id);
        //     if (currentItem == null)
        //     {
        //         throw new ResourceNotFoundException();
        //     }
        //
        //     var danmuPath = Path.Combine(currentItem.ContainingFolderPath,
        //         currentItem.FileNameWithoutExtension + ".xml");
        //     var fileMeta = _fileSystem.GetFileInfo(danmuPath);
        //     if (!fileMeta.Exists)
        //     {
        //         throw new ResourceNotFoundException();
        //     }
        //
        //     return File(System.IO.File.ReadAllBytes(danmuPath), "text/xml");
        // }
        //
        /// <summary>
        /// 查找弹幕
        /// </summary>
        // [Route("/api/danmu/search")]
        // [HttpGet]
        public async Task<IEnumerable<MediaInfo>> SearchDanmu(string keyword)
        {
            var list = new List<MediaInfo>();
        
            if (string.IsNullOrEmpty(keyword))
            {
                return list;
            }
        
            _logger.Info("_scraperManager.all = {0}", _scraperManager.All());
            foreach (var scraper in _scraperManager.All())
            {
                try
                {
                    var scraperId = Regex.Replace(scraper.ProviderId, "ID$", string.Empty).ToLower();
                    var result = await scraper.SearchForApi(keyword).ConfigureAwait(false);
                    foreach (var searchInfo in result)
                    {
                        list.Add(new MediaInfo()
                        {
                            Id = searchInfo.Id,
                            Name = searchInfo.Name,
                            // Category = searchInfo.Category,
                            // Year = searchInfo.Year == null ? string.Empty : searchInfo.Year.ToString(),
                            // EpisodeSize = searchInfo.EpisodeSize,
                            // Site = scraper.Name,
                            // SiteId = scraperId,
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{0}]Exception handled processing search movie [{1}]", scraper.Name,
                        keyword);
                }
            }
        
            return list;
        }
        //
        // /// <summary>
        // /// 查找弹幕
        // /// </summary>
        // [Route("/api/{site}/danmu/search")]
        // [HttpGet]
        // public async Task<IEnumerable<MediaInfo>> SearchDanmuBySite(string site, string keyword)
        // {
        //     var list = new List<MediaInfo>();
        //
        //     if (string.IsNullOrEmpty(keyword))
        //     {
        //         return list;
        //     }
        //
        //
        //     foreach (var scraper in _scraperManager.All())
        //     {
        //         try
        //         {
        //             var scraperId = Regex.Replace(scraper.ProviderId, "ID$", string.Empty).ToLower();
        //             if (scraperId != site)
        //             {
        //                 continue;
        //             }
        //
        //             var result = await scraper.SearchForApi(keyword).ConfigureAwait(false);
        //             foreach (var searchInfo in result)
        //             {
        //                 list.Add(new MediaInfo()
        //                 {
        //                     Id = searchInfo.Id,
        //                     Name = searchInfo.Name,
        //                     Category = searchInfo.Category,
        //                     Year = searchInfo.Year == null ? string.Empty : searchInfo.Year.ToString(),
        //                     EpisodeSize = searchInfo.EpisodeSize,
        //                     Site = scraper.Name,
        //                     SiteId = scraperId,
        //                 });
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogError(ex, "[{0}]Exception handled processing search movie [{1}]", scraper.Name,
        //                 keyword);
        //         }
        //     }
        //
        //     return list;
        // }
        //
        // /// <summary>
        // /// 查找弹幕
        // /// </summary>
        // [Route("/api/{site}/danmu/{id}/episodes")]
        // [HttpGet]
        // public async Task<IEnumerable<EpisodeInfo>> GetDanmuEpisodesBySite(string site, string id)
        // {
        //     var list = new List<EpisodeInfo>();
        //
        //     if (string.IsNullOrEmpty(id))
        //     {
        //         return list;
        //     }
        //
        //
        //     foreach (var scraper in _scraperManager.All())
        //     {
        //         try
        //         {
        //             var scraperId = Regex.Replace(scraper.ProviderId, "ID$", string.Empty).ToLower();
        //             if (scraperId != site)
        //             {
        //                 continue;
        //             }
        //
        //             var result = await scraper.GetEpisodesForApi(id).ConfigureAwait(false);
        //             foreach (var (ep, idx) in result.WithIndex())
        //             {
        //                 list.Add(new EpisodeInfo()
        //                 {
        //                     Id = ep.Id,
        //                     CommentId = ep.CommentId,
        //                     Number = idx + 1,
        //                     Title = ep.Title,
        //                 });
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogError(ex, "[{0}]Exception handled processing get episodes [{1}]", scraper.Name, id);
        //         }
        //     }
        //
        //     return list;
        // }
        //
        //
        // /// <summary>
        // /// 下载弹幕.
        // /// </summary>
        // [Route("/api/{site}/danmu/{cid}/download")]
        // [HttpGet]
        // public async Task<ActionResult> DownloadByCommentID(string site, string cid)
        // {
        //     if (string.IsNullOrEmpty(cid))
        //     {
        //         throw new ResourceNotFoundException();
        //     }
        //
        //     foreach (var scraper in this._scraperManager.All())
        //     {
        //         var scraperId = Regex.Replace(scraper.ProviderId, "ID$", string.Empty).ToLower();
        //         if (scraperId == site)
        //         {
        //             var danmaku = await scraper.DownloadDanmuForApi(cid).ConfigureAwait(false);
        //             if (danmaku != null)
        //             {
        //                 var bytes = danmaku.ToXml();
        //                 return File(bytes, "text/xml");
        //             }
        //         }
        //     }
        //
        //     throw new ResourceNotFoundException();
        // }
        //
        // /// <summary>
        // /// 跳转链接.
        // /// </summary>
        // [Route("goto")]
        // [HttpGet]
        // public RedirectResult GoTo(string provider, string id, string type)
        // {
        //     var url = $"/";
        //     switch (provider)
        //     {
        //         case "bilibili":
        //             if (id.StartsWith("BV"))
        //             {
        //                 url = $"https://www.bilibili.com/video/{id}/";
        //             }
        //             else
        //             {
        //                 if (type == "movie")
        //                 {
        //                     url = $"https://www.bilibili.com/bangumi/play/ep{id}";
        //                 }
        //                 else
        //                 {
        //                     url = $"https://www.bilibili.com/bangumi/play/ss{id}";
        //                 }
        //             }
        //
        //             break;
        //         default:
        //             break;
        //     }
        //
        //     return Redirect(url);
        // }
        //
        //
        /// <summary>
        /// 重新获取对应的弹幕id.
        /// </summary>
        /// <returns>请求结果</returns>
        public Task<String> Refresh(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ResourceNotFoundException();
            }
        
            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                throw new ResourceNotFoundException();
            }
        
            if (item is Movie || item is Season)
            {
                _logger.Info("Movie {0}, {1}", item.Name, item.GetType());
                _libraryManagerEventsHelper.QueueItem(item, Model.EventType.Add);
                _libraryManagerEventsHelper.QueueItem(item, Model.EventType.Update);
                _libraryManagerEventsHelper.QueueItem(item, Model.EventType.Update);
            }
            else if (item is Episode)
            {
                _logger.Info("Episode {0}, {1}", item.Name, item.GetType());
                _libraryManagerEventsHelper.QueueItem(item, Model.EventType.Update);
            }
            else if (item is Series)
            {
                var seasons = ((Series)item).GetSeasons(null, new DtoOptions(false));
                foreach (var season in seasons)
                {
                    _logger.Info("season = {0}, type={1}, Guid.Empty={2}", season.Name, season.GetType(), Guid.Empty.Equals(season.Id));
                    _libraryManagerEventsHelper.QueueItem(season, Model.EventType.Add);
                    _libraryManagerEventsHelper.QueueItem(season, Model.EventType.Update);
                    _libraryManagerEventsHelper.QueueItem(season, Model.EventType.Update);
                }
            }
        
            return Task.FromResult("ok");
        }
    }
}