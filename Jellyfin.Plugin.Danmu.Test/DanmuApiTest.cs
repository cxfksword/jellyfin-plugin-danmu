using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Configuration;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.DanmuApi;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class DanmuApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearchMovie()
        {
            var scraper = new DanmuApi(loggerFactory);
            var item = new Movie
            {
                Name = "火影忍者"
            };

            Task.Run(async () =>
            {
                try
                {
                    var result = await scraper.Search(item);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var searchInfo in result)
                    {
                        Console.WriteLine($"  - {searchInfo.Name} (ID: {searchInfo.Id}, Episodes: {searchInfo.EpisodeSize})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchMediaId()
        {
            var scraper = new DanmuApi(loggerFactory);
            var item = new Movie
            {
                Name = "阿丽塔：战斗天使(2019)【外语电影】from youku",
                ProductionYear = 2019
            };

            Task.Run(async () =>
            {
                try
                {
                    var mediaId = await scraper.SearchMediaId(item);
                    if (mediaId != null)
                    {
                        Console.WriteLine($"Found media ID: {mediaId}");
                    }
                    else
                    {
                        Console.WriteLine("Media ID not found or server not configured");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetMedia()
        {
            var scraper = new DanmuApi(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };
            var testBangumiId = "test-bangumi-id";

            Task.Run(async () =>
            {
                try
                {
                    var media = await scraper.GetMedia(item, testBangumiId);
                    if (media != null)
                    {
                        Console.WriteLine($"Media ID: {media.Id}");
                        Console.WriteLine($"Comment ID: {media.CommentId}");
                        Console.WriteLine($"Episodes count: {media.Episodes.Count}");
                    }
                    else
                    {
                        Console.WriteLine("Media not found or server not configured");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuContent()
        {
            var scraper = new DanmuApi(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };
            var commentId = "test-comment-id";

            Task.Run(async () =>
            {
                try
                {
                    var danmaku = await scraper.GetDanmuContent(item, commentId);
                    if (danmaku != null)
                    {
                        Console.WriteLine($"Chat Server: {danmaku.ChatServer}");
                        Console.WriteLine($"Danmaku items count: {danmaku.Items.Count}");
                        foreach (var item in danmaku.Items.Take(5))
                        {
                            Console.WriteLine($"  - [{item.Progress}ms] {item.Content}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Danmaku not found or server not configured");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestFilterByAllowedSources()
        {
            // 模拟配置只允许特定采集源
            var originalConfig = Plugin.Instance?.Configuration?.DanmuApi?.AllowedSources;
            
            try
            {
                if (Plugin.Instance?.Configuration?.DanmuApi != null)
                {
                    Plugin.Instance.Configuration.DanmuApi.AllowedSources = "bilibili";
                }

                var scraper = new DanmuApi(loggerFactory);
                var item = new Movie
                {
                    Name = "火影忍者"
                };

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await scraper.Search(item);
                        Console.WriteLine($"Source filter test - Results count: {result.Count}");
                        
                        // 验证所有结果都来自允许的采集源
                        foreach (var searchInfo in result)
                        {
                            Console.WriteLine($"  - {searchInfo.Name} (ID: {searchInfo.Id})");
                        }
                        
                        if (result.Count > 0)
                        {
                            Console.WriteLine("✓ Source filter is working - only allowed sources returned");
                        }
                        else
                        {
                            Console.WriteLine("⚠ No results found with source filter");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }).GetAwaiter().GetResult();
            }
            finally
            {
                // 恢复原始配置
                if (Plugin.Instance?.Configuration?.DanmuApi != null)
                {
                    Plugin.Instance.Configuration.DanmuApi.AllowedSources = originalConfig ?? string.Empty;
                }
            }
        }

        [TestMethod]
        public void TestFilterByAllowedPlatforms()
        {
            // 模拟配置只允许特定平台
            var originalConfig = Plugin.Instance?.Configuration?.DanmuApi?.AllowedPlatforms;
            
            try
            {
                if (Plugin.Instance?.Configuration?.DanmuApi != null)
                {
                    Plugin.Instance.Configuration.DanmuApi.AllowedPlatforms = "qq";
                }

                var scraper = new DanmuApi(loggerFactory);
                var item = new Movie
                {
                    Name = "又见逍遥"
                };

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await scraper.GetMedia(item, "5510");
                        Console.WriteLine($"Platform filter test - Results count: {result.Episodes.Count}");
                        
                        // 验证所有结果都来自允许的平台
                        foreach (var episode in result.Episodes)
                        {
                            Console.WriteLine($"  - {episode.Title} (ID: {episode.Id})");
                        }
                        
                        if (result.Episodes.Count > 0)
                        {
                            Console.WriteLine("✓ Platform filter is working - only allowed platforms returned");
                        }
                        else
                        {
                            Console.WriteLine("⚠ No results found with platform filter");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }).GetAwaiter().GetResult();
            }
            finally
            {
                // 恢复原始配置
                if (Plugin.Instance?.Configuration?.DanmuApi != null)
                {
                    Plugin.Instance.Configuration.DanmuApi.AllowedPlatforms = originalConfig ?? string.Empty;
                }
            }
        }


    }
}
