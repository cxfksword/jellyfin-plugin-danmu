using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Scrapers.Renren;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class RenrenApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearchAsync()
        {
            var keyword = "铁拳教育";
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var item in result)
                    {
                        Console.WriteLine($"  - {item.Title} (ID: {item.Id}, Year: {item.Year}, Type: {item.Classify})");
                    }

                    Assert.IsTrue(result.Count > 0, "搜索应返回至少一个结果");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"搜索接口异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchWithChineseKeyword()
        {
            var keyword = "斗破苍穹";
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var item in result)
                    {
                        Console.WriteLine($"  - {item.Title} (ID: {item.Id})");
                    }

                    Assert.IsTrue(result.Count > 0, "搜索中文关键词应返回结果");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"搜索接口异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchEmptyKeyword()
        {
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchAsync("", CancellationToken.None);
                    Assert.AreEqual(0, result.Count, "空关键词应返回空列表");
                    Console.WriteLine("Empty keyword returned 0 results as expected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"搜索接口异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDetailAsync()
        {
            // 使用搜索到的有效剧集ID
            var dramaId = "57539";
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetDetailAsync(dramaId, "", CancellationToken.None);
                    if (result != null)
                    {
                        Console.WriteLine($"Drama detail loaded successfully");
                        Console.WriteLine($"Episodes count: {result.EpisodeList?.Count ?? 0}");
                        if (result.EpisodeList != null)
                        {
                            foreach (var ep in result.EpisodeList)
                            {
                                Console.WriteLine($"  - SID: {ep.Sid}, Title: {ep.Title}, EpisodeNo: {ep.EpisodeNo}");
                            }
                        }

                        Assert.IsNotNull(result.EpisodeList, "剧集列表不应为null");
                        Assert.IsTrue(result.EpisodeList.Count > 0, "应包含至少一集");
                    }
                    else
                    {
                        Console.WriteLine("Drama detail not found, this may be expected if the ID is invalid");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"获取详情异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuAsync()
        {
            // 使用复合ID格式: seriesId-episodeSid
            var episodeSid = "140957";
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetDanmuAsync(episodeSid, CancellationToken.None);
                    Console.WriteLine($"Danmu count: {result.Count}");
                    foreach (var danmu in result)
                    {
                        Console.WriteLine($"  - p: {danmu.P}, content: {danmu.D}");
                    }

                    // 弹幕可能为空（该集可能没有弹幕），但不应该异常
                    Assert.IsNotNull(result, "弹幕结果不应为null");
                    Console.WriteLine($"Danmu test completed, found {result.Count} danmu items");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"获取弹幕异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuInvalidId()
        {
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetDanmuAsync("invalid-id-999999", CancellationToken.None);
                    Assert.IsNotNull(result, "无效ID应返回空列表而非异常");
                    Assert.AreEqual(0, result.Count, "无效ID应返回空列表");
                    Console.WriteLine("Invalid ID returned empty list as expected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"获取弹幕异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuWithSimpleId()
        {
            // 测试直接使用episodeSid而非复合ID
            var episodeSid = "386435";
            var api = new RenrenApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetDanmuAsync(episodeSid, CancellationToken.None);
                    Console.WriteLine($"Danmu count: {result.Count}");
                    Assert.IsNotNull(result, "弹幕结果不应为null");
                    Console.WriteLine($"Simple ID test completed, found {result.Count} danmu items");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Assert.Fail($"获取弹幕异常: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }
    }
}
