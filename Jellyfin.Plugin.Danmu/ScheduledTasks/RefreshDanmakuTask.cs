using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.ScheduledTasks
{
    public class RefreshDanmuTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly BilibiliApi _api;
        private readonly ILogger _logger;


        public string Key => $"{Plugin.Instance.Name}RefreshDanmu";

        public string Name => "刷新弹幕文件";

        public string Description => $"根据视频b站元数据下载最新的弹幕文件。";

        public string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshDanmuTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="api">Instance of the <see cref="BilibiliApi"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public RefreshDanmuTask(ILoggerFactory loggerFactory, BilibiliApi api, ILibraryManager libraryManager)
        {
            _logger = loggerFactory.CreateLogger<RefreshDanmuTask>();
            _libraryManager = libraryManager;
            _api = api;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Monday,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Yield();

            progress?.Report(0);

            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                HasAnyProviderId = new Dictionary<string, string> { { Plugin.ProviderId, string.Empty } },
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode }
            }).ToList();

            _logger.LogInformation("Update danmu for {0} videos.", items.Count);

            var successCount = 0;
            var failCount = 0;
            foreach (var (item, idx) in items.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                try
                {
                    // TODO: 不支持bv刷新处理
                    var providerVal = item.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    // 视频也支持指定的BV号
                    if (providerVal.StartsWith("BV", StringComparison.CurrentCulture))
                    {
                        var bvid = providerVal;

                        // 下载弹幕xml文件
                        var bytes = await _api.GetDanmaContentAsync(bvid, CancellationToken.None).ConfigureAwait(false);
                        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
                        await File.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);
                        successCount++;
                    }
                    else
                    {
                        var epId = providerVal.ToLong();
                        if (epId <= 0)
                        {
                            _logger.LogWarning("Update danmu for video {0}: epId is empty", item.Name);
                            failCount++;
                            continue;
                        }

                        // 下载弹幕xml文件
                        var bytes = await _api.GetDanmaContentAsync(epId, cancellationToken).ConfigureAwait(false);
                        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
                        await File.WriteAllBytesAsync(danmuPath, bytes, cancellationToken).ConfigureAwait(false);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Update danmu for video {0}: {1}", item.Name, ex.Message);
                    failCount++;
                }

                // 延迟200毫秒，避免请求太频繁
                Thread.Sleep(200);
            }

            progress?.Report(100);
            _logger.LogInformation("Exectue task completed. success: {0} fail: {1}", successCount, failCount);
        }
    }
}
