using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class BilibiliApiTest
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            }));

        [TestMethod]
        public void TestSearch()
        {
            var keyword = "凡人修仙传";
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _bilibiliApi.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchFrequently()
        {
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var keyword = "哆啦A梦 第四季";
                    var result = await _bilibiliApi.SearchAsync(keyword, CancellationToken.None);
                    keyword = "哆啦A梦";
                    result = await _bilibiliApi.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetSeasonAsync()
        {
            var seasonId = 28747;
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _bilibiliApi.GetSeasonAsync(seasonId, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetVideoByBvidAsync()
        {
            var bvid = "BV1vs411U78W";
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _bilibiliApi.GetVideoByBvidAsync(bvid, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetVideoByBvidForCollectionAsync()
        {
            var bvid = "BV1z34y1h7yQ";
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _bilibiliApi.GetVideoByBvidAsync(bvid, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetVideoByAvidAsync()
        {
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var avid = "av5048623";
                    var result = await _bilibiliApi.GetVideoByAvidAsync(avid, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmuContentByProtoAsync()
        {
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var aid = 5048623;
                    var cid = 9708007;
                    var result = await _bilibiliApi.GetDanmuContentByProtoAsync(aid, cid, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }
    }
}
