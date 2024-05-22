using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class IqiyiApiTest
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
            var api = new IqiyiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var keyword = "又见逍遥";
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


        [TestMethod]
        public void TestGetVideo()
        {
            var api = new IqiyiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var id = "o5e8yl8378"; // 综艺
                    // var id = "19tfhh8axvc"; // 电视剧
                    // var id = "1e54n0pt5ro"; // 电影
                    var result = await api.GetVideoAsync(id, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetZongyiEpisodes()
        {
            var api = new IqiyiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var albumId = "252894801";
                    var result = await api.GetZongyiEpisodesAsync(albumId, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


        [TestMethod]
        public void TestGetDanmuContentByMat()
        {
            var api = new IqiyiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var vid = "132987200";
                    var result = await api.GetDanmuContentByMatAsync(vid, 1, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetDanmu()
        {
            var api = new IqiyiApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var vid = "132987200";
                    var result = await api.GetDanmuContentAsync(vid, CancellationToken.None);
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
