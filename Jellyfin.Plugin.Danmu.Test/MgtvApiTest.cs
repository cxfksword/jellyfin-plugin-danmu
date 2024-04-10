using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class MgtvApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearch()
        {
            Task.Run(async () =>
            {
                try
                {
                    var keyword = "与凤行";
                    var api = new MgtvApi(loggerFactory);
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
                    // var id = "310102";  // 综艺
                    var id = "626407";  // 电视剧
                    var api = new MgtvApi(loggerFactory);
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
        public void TestGetDanmu()
        {

            Task.Run(async () =>
            {
                try
                {
                    var cid = "514446";
                    var vid = "18053294";
                    var api = new MgtvApi(loggerFactory);
                    var result = await api.GetDanmuContentAsync(cid, vid, CancellationToken.None);
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
