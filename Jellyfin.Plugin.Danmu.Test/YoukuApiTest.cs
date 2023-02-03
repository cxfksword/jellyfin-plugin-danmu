using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Youku;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class YoukuApiTest
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
            var keyword = "一拳超人";
            var api = new YoukuApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
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
            var api = new YoukuApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var vid = "0b39c5b6569311e5b2ad";
                    var result = await api.GetVideoAsync(vid, CancellationToken.None);
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
            var api = new YoukuApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var vid = "XMTM1MTc4MDU3Ng==";
                    var result = await api.GetDanmuContentByMatAsync(vid, 0, CancellationToken.None);
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
            var api = new YoukuApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var vid = "XMTM1MTc4MDU3Ng==";
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
