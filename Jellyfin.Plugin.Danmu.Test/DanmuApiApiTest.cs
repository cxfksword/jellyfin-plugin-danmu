using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Scrapers.DanmuApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class DanmuApiApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearchAsync()
        {
            var keyword = "火影忍者";
            var api = new DanmuApiApi(loggerFactory);

            // 注意：此测试需要实际的服务器 URL，否则会返回空结果
            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine($"Search results count: {result.Count}");
                    foreach (var anime in result)
                    {
                        Console.WriteLine($"  - {anime.AnimeTitle} (ID: {anime.BangumiId})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetBangumiAsync()
        {
            var bangumiId = "mzc00200nc1cbum";
            var api = new DanmuApiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetBangumiAsync(bangumiId, CancellationToken.None);
                    if (result != null)
                    {
                        Console.WriteLine($"Bangumi: {result.AnimeTitle}");
                        Console.WriteLine($"Episodes count: {result.Episodes.Count}");
                        foreach (var episode in result.Episodes.Take(5))
                        {
                            Console.WriteLine($"  - {episode.EpisodeId}: {episode.EpisodeTitle}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Bangumi not found or server not configured");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetCommentsAsync()
        {
            var commentId = "14435";
            var api = new DanmuApiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetCommentsAsync(commentId, CancellationToken.None);
                    Console.WriteLine($"Comments count: {result.Count}");
                    foreach (var comment in result.Take(5))
                    {
                        Console.WriteLine($"  - {comment.M} (CID: {comment.Cid})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

    }
}
