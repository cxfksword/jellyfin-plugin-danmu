using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class DandanApiTest : BaseTest
    {
        [TestMethod]
        public void TestSearch()
        {
            var keyword = "混沌武士";
            var _api = new DandanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _api.SearchAsync(keyword, CancellationToken.None);
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
            var _api = new DandanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var keyword = "剑风传奇";
                    var result = await _api.SearchAsync(keyword, CancellationToken.None);
                    keyword = "哆啦A梦";
                    result = await _api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetAnimeAsync()
        {
            long animeID = 11829;
            var _api = new DandanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _api.GetAnimeAsync(animeID, CancellationToken.None);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetCommentsAsync()
        {
            long epId = 118290001;
            var _api = new DandanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await _api.GetCommentsAsync(epId, CancellationToken.None);
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
