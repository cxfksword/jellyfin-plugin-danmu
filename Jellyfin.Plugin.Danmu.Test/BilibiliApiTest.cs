using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
            var bvid = "BV1huGizKEZd";
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
        public void TestGetDanmuContentByCidAsync()
        {
            var _bilibiliApi = new BilibiliApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var cid = 541370210;
                    var bytes = await _bilibiliApi.GetDanmuContentByCidAsync(cid, CancellationToken.None);
                    var doc = new XmlDocument();
                    doc.LoadXml(System.Text.Encoding.UTF8.GetString(bytes));
                    var nodes = doc.GetElementsByTagName("d");
                    Console.WriteLine(nodes.Count);
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
                    var aid = 381941309;
                    var cid = 541370210;
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
