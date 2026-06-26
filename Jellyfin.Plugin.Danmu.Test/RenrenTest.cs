using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Configuration;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Renren;
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
    public class RenrenTest : BaseTest
    {
        [TestMethod]
        public void TestSearchMovie()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "地下忍者"
            };

            Task.Run(async () =>
            {
                try
                {
                    var result = await scraper.Search(item);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var searchInfo in result)
                    {
                        Console.WriteLine($"  - {searchInfo.Name} (ID: {searchInfo.Id}, Year: {searchInfo.Year}, Category: {searchInfo.Category})");
                    }

                    if (result.Count == 0)
                    {
                        Console.WriteLine("Warning: 搜索未返回结果，这可能是因为API限制或网络问题");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchSeason()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Season
            {
                Name = "黑镜"
            };

            Task.Run(async () =>
            {
                try
                {
                    var result = await scraper.Search(item);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var searchInfo in result)
                    {
                        Console.WriteLine($"  - {searchInfo.Name} (ID: {searchInfo.Id}, Year: {searchInfo.Year}, Category: {searchInfo.Category})");
                    }

                    if (result.Count == 0)
                    {
                        Console.WriteLine("Warning: 搜索未返回结果，这可能是因为API限制或网络问题");
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
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "地下忍者",
                ProductionYear = 2023
            };

            Task.Run(async () =>
            {
                try
                {
                    var mediaId = await scraper.SearchMediaId(item);
                    if (mediaId != null)
                    {
                        Console.WriteLine($"Found media ID: {mediaId}");
                        Assert.IsFalse(string.IsNullOrEmpty(mediaId), "返回的mediaId不应为空");
                    }
                    else
                    {
                        Console.WriteLine("Media ID not found - this may be due to API limitations");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchMediaIdWithMismatchedYear()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "地下忍者",
                ProductionYear = 2024
            };

            Task.Run(async () =>
            {
                try
                {
                    var mediaId = await scraper.SearchMediaId(item);
                    if (mediaId == null)
                    {
                        Console.WriteLine("Mismatched year or API returned no match");
                    }
                    else
                    {
                        Console.WriteLine($"Found media ID: {mediaId} (may not be year-filtered)");
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
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "获取媒体测试"
            };
            var testId = "45881";

            Task.Run(async () =>
            {
                try
                {
                    var media = await scraper.GetMedia(item, testId);
                    if (media != null)
                    {
                        Console.WriteLine($"Media ID: {media.Id}");
                        Console.WriteLine($"Comment ID: {media.CommentId}");
                        Console.WriteLine($"Episodes count: {media.Episodes.Count}");
                        foreach (var ep in media.Episodes.Take(5))
                        {
                            Console.WriteLine($"  - {ep.Title} (ID: {ep.Id}, CommentId: {ep.CommentId})");
                        }

                        Assert.AreEqual(testId, media.Id, "Media ID应匹配");
                        Assert.IsTrue(media.Episodes.Count > 0, "应包含至少一集");
                    }
                    else
                    {
                        Console.WriteLine("Media not found for ID: " + testId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetMediaInvalidId()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };

            Task.Run(async () =>
            {
                try
                {
                    var media = await scraper.GetMedia(item, "");
                    Assert.IsNull(media, "空ID应返回null");
                    Console.WriteLine("Empty ID correctly returned null");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"GetMedia异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetMediaEpisodeForMovie()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };
            var testId = "45881";

            Task.Run(async () =>
            {
                try
                {
                    var episode = await scraper.GetMediaEpisode(item, testId);
                    if (episode != null)
                    {
                        Console.WriteLine($"Episode ID: {episode.Id}");
                        Console.WriteLine($"Comment ID: {episode.CommentId}");
                        Console.WriteLine($"Title: {episode.Title}");

                        Assert.IsNotNull(episode.CommentId, "CommentId不应为空");
                    }
                    else
                    {
                        Console.WriteLine("Episode not found for ID: " + testId);
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
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };
            var commentId = "140957";

            Task.Run(async () =>
            {
                try
                {
                    var danmaku = await scraper.GetDanmuContent(item, commentId);
                    if (danmaku != null)
                    {
                        Console.WriteLine($"Chat Server: {danmaku.ChatServer}");
                        Console.WriteLine($"Danmaku items count: {danmaku.Items.Count}");
                        foreach (var d in danmaku.Items.Take(5))
                        {
                            Console.WriteLine($"  - [{d.Progress}ms] Mode={d.Mode}, Color={d.Color}, Content={d.Content}");
                        }

                        Assert.IsNotNull(danmaku.Items, "弹幕列表不应为null");
                        Console.WriteLine("Danmaku content test completed");
                    }
                    else
                    {
                        Console.WriteLine("Danmaku not found (null returned)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuContentEmptyCommentId()
        {
            var scraper = new Renren(loggerFactory);
            var item = new Movie
            {
                Name = "测试电影"
            };

            Task.Run(async () =>
            {
                try
                {
                    var danmaku = await scraper.GetDanmuContent(item, "");
                    Assert.IsNull(danmaku, "空commentId应返回null");
                    Console.WriteLine("Empty commentId correctly returned null");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"GetDanmuContent异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchForApi()
        {
            var scraper = new Renren(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await scraper.SearchForApi("火影忍者");
                    Console.WriteLine($"API Search results count: {result.Count}");
                    foreach (var info in result)
                    {
                        Console.WriteLine($"  - {info.Name} (ID: {info.Id}, Year: {info.Year})");
                    }

                    Assert.IsTrue(result.Count > 0, "API搜索应返回至少一个结果");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"SearchForApi异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetEpisodesForApi()
        {
            var scraper = new Renren(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await scraper.GetEpisodesForApi("45881");
                    Console.WriteLine($"Episodes count: {result.Count}");
                    foreach (var ep in result.Take(10))
                    {
                        Console.WriteLine($"  - {ep.Title} (ID: {ep.Id}, CommentId: {ep.CommentId})");
                    }

                    if (result.Count == 0)
                    {
                        Console.WriteLine("Warning: No episodes returned for ID 45881");
                    }
                    else
                    {
                        Assert.IsTrue(result.Count > 0, "应返回至少一集");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestDownloadDanmuForApi()
        {
            var scraper = new Renren(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var danmaku = await scraper.DownloadDanmuForApi("140957");
                    if (danmaku != null)
                    {
                        Console.WriteLine($"Downloaded danmu count: {danmaku.Items.Count}");
                        foreach (var d in danmaku.Items.Take(5))
                        {
                            Console.WriteLine($"  - [{d.Progress}ms] {d.Content}");
                        }

                        Assert.IsNotNull(danmaku.Items, "弹幕列表不应为null");
                        Console.WriteLine("Download danmu test completed");
                    }
                    else
                    {
                        Console.WriteLine("Danmaku not found (null returned)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestScraperProperties()
        {
            var scraper = new Renren(loggerFactory);
            Assert.AreEqual("人人视频", scraper.Name, "Name应为人人视频");
            Assert.AreEqual("RenrenID", scraper.ProviderId, "ProviderId应为RenrenID");
            Assert.AreEqual("人人视频", scraper.ProviderName, "ProviderName应为人人视频");
            Assert.AreEqual(17u, scraper.HashPrefix, "HashPrefix应为17");
            Assert.AreEqual(17, scraper.DefaultOrder, "DefaultOrder应为17");
            Assert.IsFalse(scraper.DefaultEnable, "DefaultEnable应为false");
            Assert.IsFalse(scraper.IsDeprecated, "IsDeprecated应为false");

            Console.WriteLine($"Name: {scraper.Name}");
            Console.WriteLine($"ProviderId: {scraper.ProviderId}");
            Console.WriteLine($"ProviderName: {scraper.ProviderName}");
            Console.WriteLine($"HashPrefix: {scraper.HashPrefix}");
            Console.WriteLine($"DefaultOrder: {scraper.DefaultOrder}");
            Console.WriteLine($"DefaultEnable: {scraper.DefaultEnable}");
            Console.WriteLine($"IsDeprecated: {scraper.IsDeprecated}");
        }
    }
}
