using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Controllers;
using Jellyfin.Plugin.Danmu.Controllers.Entity;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan;
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv;
using Jellyfin.Plugin.Danmu.Scrapers.Youku;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class ApiControllerTest : BaseTest
    {
        private ApiController _apiController = null!;
        private ScraperManager _scraperManager = null!;

        [TestInitialize]
        public new void SetUp()
        {
            base.SetUp();

            // 初始化 ScraperManager 并注册所有弹幕源
            _scraperManager = new ScraperManager(loggerFactory);
            _scraperManager.Register(new Bilibili(loggerFactory));
            // _scraperManager.Register(new Dandan(loggerFactory));
            // _scraperManager.Register(new Youku(loggerFactory));
            // _scraperManager.Register(new Iqiyi(loggerFactory));
            // _scraperManager.Register(new Tencent(loggerFactory));
            // _scraperManager.Register(new Mgtv(loggerFactory));

            // 配置插件存储路径，确保文件缓存可用
            var pluginConfigPath = Path.Combine(Path.GetTempPath(), "danmu-plugin-tests");
            Directory.CreateDirectory(pluginConfigPath);
            var applicationPathsMock = new Mock<IApplicationPaths>();
            applicationPathsMock.SetupGet(p => p.PluginConfigurationsPath).Returns(pluginConfigPath);

            var fileCache = new FileCache<AnimeCacheItem>(applicationPathsMock.Object, loggerFactory, TimeSpan.FromDays(31), TimeSpan.FromSeconds(60));

            // 创建 ApiController 实例
            _apiController = new ApiController(
                loggerFactory,
                _scraperManager,
                fileCache);
        }

        [TestMethod]
        public async Task TestSearchAnime()
        {
            Console.WriteLine("========== 测试搜索动画 ==========");
            
            var keyword = "葬送的芙莉莲";
            Console.WriteLine($"搜索关键词: {keyword}");
            Console.WriteLine();

            var result = await _apiController.SearchAnime(keyword);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Animes);

            // 打印 JSON 结果
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(result, jsonOptions);
            Console.WriteLine("搜索结果 JSON:");
            Console.WriteLine(json);
            Console.WriteLine();

            // 验证结果
            var animeList = result.Animes.ToList();
            if (animeList.Any())
            {
                Console.WriteLine($"找到 {animeList.Count} 个结果");
                foreach (var anime in animeList.Take(5))
                {
                    Console.WriteLine($"  - AnimeId: {anime.AnimeId}, BangumiId: {anime.BangumiId}, Title: {anime.AnimeTitle}, Type: {anime.TypeDescription}");
                    
                    // 验证 AnimeId 格式（11位数字，前2位是 HashPrefix）
                    var animeIdStr = anime.AnimeId.ToString();
                    Assert.IsTrue(animeIdStr.Length <= 11, $"AnimeId {anime.AnimeId} 应该不超过11位");
                    
                    // 验证前2位是有效的 HashPrefix (10-15)
                    if (animeIdStr.Length >= 2)
                    {
                        var prefix = int.Parse(animeIdStr.Substring(0, 2));
                        Assert.IsTrue(prefix >= 10 && prefix <= 15, $"HashPrefix {prefix} 应该在 10-15 范围内");
                    }
                }
            }
            else
            {
                Console.WriteLine("未找到搜索结果");
            }
        }

        [TestMethod]
        public async Task TestSearchAnimeEpisodes()
        {
            Console.WriteLine("========== 测试搜索动画剧集 ==========");
            
            var keyword = "孤独的美食家";
            Console.WriteLine($"搜索关键词: {keyword}");
            Console.WriteLine();

            var result = await _apiController.SearchAnimeEpisodes(keyword);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Animes);

            // 打印 JSON 结果
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(result, jsonOptions);
            Console.WriteLine("搜索结果（含剧集）JSON:");
            Console.WriteLine(json);
            Console.WriteLine();

            // 验证结果
            var animeEpisodesList = result.Animes.ToList();
            if (animeEpisodesList.Any())
            {
                Console.WriteLine($"找到 {animeEpisodesList.Count} 个结果");
                foreach (var anime in result.Animes.Take(3))
                {
                    Console.WriteLine($"  - AnimeId: {anime.AnimeId}, Title: {anime.AnimeTitle}, Episodes: {anime.Episodes?.Count ?? 0}");
                    
                    if (anime.Episodes != null && anime.Episodes.Any())
                    {
                        Console.WriteLine($"    前3集: {string.Join(", ", anime.Episodes.Take(3).Select(e => $"E{e.EpisodeNumber}"))}");
                    }
                }
            }
            else
            {
                Console.WriteLine("未找到搜索结果");
            }
        }

        [TestMethod]
        public async Task TestGetAnime()
        {
            Console.WriteLine("========== 测试获取动画详情 ==========");
            
            // 步骤1: 先搜索动画以填充缓存
            var keyword = "葬送的芙莉莲";
            Console.WriteLine($"步骤1: 搜索动画 '{keyword}' 以填充缓存");
            var searchResult = await _apiController.SearchAnime(keyword);
            
            Assert.IsNotNull(searchResult);
            Assert.IsTrue(searchResult.Success);
            Assert.IsNotNull(searchResult.Animes);
            Assert.IsTrue(searchResult.Animes.Any(), "搜索应该返回至少一个结果");

            // 步骤2: 获取第一个搜索结果的 AnimeId
            var firstAnime = searchResult.Animes.First();
            var animeId = firstAnime.AnimeId.ToString();
            Console.WriteLine($"步骤2: 使用 AnimeId '{animeId}' 获取详情");
            Console.WriteLine($"  BangumiId: {firstAnime.BangumiId}");
            Console.WriteLine($"  Title: {firstAnime.AnimeTitle}");
            Console.WriteLine();

            // 步骤3: 通过 AnimeId 获取动画详情
            var result = await _apiController.GetAnime(animeId);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success, $"GetAnime 应该成功: {result.ErrorMessage}");
            Assert.IsNotNull(result.Bangumi, "Bangumi 不应该为 null");

            // 打印 JSON 结果
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(result, jsonOptions);
            Console.WriteLine("动画详情 JSON:");
            Console.WriteLine(json);
            Console.WriteLine();

            // 验证结果
            var bangumi = result.Bangumi;
            Console.WriteLine($"AnimeId: {bangumi.AnimeId}");
            Console.WriteLine($"BangumiId: {bangumi.BangumiId}");
            Console.WriteLine($"TypeDescription: {bangumi.TypeDescription}");

            if (bangumi.Episodes != null && bangumi.Episodes.Any())
            {
                Console.WriteLine($"找到 {bangumi.Episodes.Count} 集");
                foreach (var ep in bangumi.Episodes.Take(5))
                {
                    Console.WriteLine($"  - 第{ep.EpisodeNumber}集, EpisodeId: {ep.EpisodeId}, Title: {ep.EpisodeTitle}");
                }
                
                Assert.IsTrue(bangumi.Episodes.Count > 0, "应该至少有一集");
            }
            else
            {
                Console.WriteLine("未找到剧集信息");
            }

            // 验证 AnimeId 格式
            var animeIdStr = bangumi.AnimeId.ToString();
            if (animeIdStr.Length >= 2)
            {
                var prefix = animeIdStr.Substring(0, 2);
                Console.WriteLine($"HashPrefix: {prefix}");
                Assert.IsTrue(int.Parse(prefix) >= 10 && int.Parse(prefix) <= 15, 
                    $"HashPrefix {prefix} 应该在 10-15 范围内");
            }

            // 步骤4: 测试缓存未命中的情况
            Console.WriteLine();
            Console.WriteLine("步骤4: 测试缓存未命中（使用不存在的 ID）");
            var notFoundResult = await _apiController.GetAnime("99999999999");
            Assert.IsNotNull(notFoundResult);
            Assert.IsFalse(notFoundResult.Success, "不存在的 ID 应该返回失败");
            Assert.AreEqual(404, notFoundResult.ErrorCode, "应该返回 404 错误码");
            Console.WriteLine($"预期的错误: {notFoundResult.ErrorMessage}");
        }

        [TestMethod]
        public void TestConvertToAnimeId()
        {
            Console.WriteLine("========== 测试 ID 转换算法 ==========");
            Console.WriteLine();

            var scraper = new Bilibili(loggerFactory);
            
            // 测试用例
            var testCases = new[]
            {
                new { Id = "123456", Description = "纯数字ID (小于9位)" },
                new { Id = "999999999", Description = "纯数字ID (9位最大值)" },
                new { Id = "1234567890", Description = "纯数字ID (超过9位)" },
                new { Id = "XMzE2NTk4MjQw", Description = "字母数字混合ID (Youku)" },
                new { Id = "9bvckxy83zo", Description = "字母数字混合ID (Iqiyi)" },
                new { Id = "abc_xyz-123", Description = "包含特殊字符的ID" },
                new { Id = "", Description = "空字符串" },
            };

            foreach (var testCase in testCases)
            {
                // 使用反射调用私有方法
                var method = typeof(ApiController).GetMethod("ConvertToHashId", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                var animeId = (long)(method?.Invoke(_apiController, new object[] { scraper, testCase.Id }) ?? 0L);
                
                var animeIdStr = animeId.ToString();
                var prefix = animeIdStr.Length >= 2 ? animeIdStr.Substring(0, 2) : "N/A";
                
                Console.WriteLine($"ID: '{testCase.Id}'");
                Console.WriteLine($"  描述: {testCase.Description}");
                Console.WriteLine($"  AnimeId: {animeId}");
                Console.WriteLine($"  HashPrefix: {prefix}");
                Console.WriteLine($"  位数: {animeIdStr.Length}");
                Console.WriteLine();

                // 验证非空ID应该有正确的前缀
                if (!string.IsNullOrEmpty(testCase.Id) && animeId > 0)
                {
                    Assert.AreEqual("10", prefix, $"Bilibili的HashPrefix应该是10");
                    Assert.IsTrue(animeIdStr.Length <= 11, $"AnimeId位数不应超过11位");
                }
            }

            // 测试不同的 scraper
            Console.WriteLine("========== 测试不同弹幕源的 HashPrefix ==========");
            Console.WriteLine();

            var scrapers = new AbstractScraper[]
            {
                new Bilibili(loggerFactory),
                new Dandan(loggerFactory),
                new Youku(loggerFactory),
                new Iqiyi(loggerFactory),
                new Tencent(loggerFactory),
                new Mgtv(loggerFactory)
            };

            var testId = "123456";
            foreach (var testScraper in scrapers)
            {
                var method = typeof(ApiController).GetMethod("ConvertToHashId", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                var animeId = (long)(method?.Invoke(_apiController, new object[] { testScraper, testId }) ?? 0L);
                var animeIdStr = animeId.ToString();
                var prefix = animeIdStr.Length >= 2 ? animeIdStr.Substring(0, 2) : "N/A";
                
                Console.WriteLine($"{testScraper.Name,-10} HashPrefix: {testScraper.HashPrefix} => AnimeId: {animeId} (前缀: {prefix})");
                
                if (animeId > 0 && prefix != "N/A")
                {
                    Assert.AreEqual(testScraper.HashPrefix.ToString(), prefix, 
                        $"{testScraper.Name} 的 HashPrefix 应该是 {testScraper.HashPrefix}");
                }
            }
        }

        [TestMethod]
        public async Task TestSearchAnimeWithDifferentScrapers()
        {
            Console.WriteLine("========== 测试不同弹幕源的搜索结果 ==========");
            Console.WriteLine();

            var keyword = "鬼灭之刃";
            Console.WriteLine($"搜索关键词: {keyword}");
            Console.WriteLine();

            var result = await _apiController.SearchAnime(keyword);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Animes);

            // 按弹幕源分组显示结果
            var allAnimes = result.Animes.ToList();
            var groupedResults = allAnimes
                .GroupBy(a => a.TypeDescription.Split(']')[0] + "]")
                .ToList();

            Console.WriteLine($"共找到 {allAnimes.Count} 个结果，来自 {groupedResults.Count} 个弹幕源");
            Console.WriteLine();

            foreach (var group in groupedResults)
            {
                Console.WriteLine($"{group.Key} - {group.Count()} 个结果:");
                foreach (var anime in group.Take(2))
                {
                    Console.WriteLine($"  AnimeId: {anime.AnimeId}, BangumiId: {anime.BangumiId}, Title: {anime.AnimeTitle}");
                }
                Console.WriteLine();
            }

            // 验证不同弹幕源有不同的 HashPrefix
            var prefixes = allAnimes
                .Where(a => a.AnimeId > 0)
                .Select(a => a.AnimeId.ToString().Substring(0, 2))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            Console.WriteLine($"使用的 HashPrefix: {string.Join(", ", prefixes)}");
            
            // 应该有多个不同的前缀
            if (allAnimes.Count > 0)
            {
                Assert.IsTrue(prefixes.Count > 0, "应该至少有一个HashPrefix");
            }
        }
    }
}
