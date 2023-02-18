using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class TencentApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearch()
        {
            Task.Run(async () =>
            {
                try
                {
                    var keyword = "流浪地球";
                    var api = new TencentApi(loggerFactory);
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
            Task.Run(async () =>
            {
                try
                {
                    var vid = "mzc00200koowgko";
                    var api = new TencentApi(loggerFactory);
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
        public void TestGetDanmu()
        {

            Task.Run(async () =>
            {
                try
                {
                    var vid = "a00149qxvfz";
                    var api = new TencentApi(loggerFactory);
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
