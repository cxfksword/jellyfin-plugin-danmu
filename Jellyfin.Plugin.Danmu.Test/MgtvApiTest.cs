using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                    Console.WriteLine(string.Join("\n", result.Select(x => $"{x.Id} | {x.Title} | {x.TypeName} | {x.Year} | {x.VideoCount}")));

                    Assert.IsTrue(result.Count > 0, "搜索应返回结果");
                    var first = result.First();
                    Assert.IsFalse(string.IsNullOrEmpty(first.Id), "Id不能为空");
                    Assert.IsFalse(string.IsNullOrEmpty(first.Title), "Title不能为空");
                    Assert.IsFalse(string.IsNullOrEmpty(first.TypeName), "TypeName不能为空");
                    Assert.IsTrue(first.Year > 0, "Year应大于0");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchMovie()
        {
            Task.Run(async () =>
            {
                try
                {
                    // 用户反馈搜索电影报403的场景
                    var keyword = "龙女奇遇记";
                    var api = new MgtvApi(loggerFactory);
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(string.Join("\n", result.Select(x => $"{x.Id} | {x.Title} | {x.TypeName} | {x.Year} | {x.VideoCount}")));

                    Assert.IsTrue(result.Any(x => x.TypeName == "电影"), "应包含电影类型的结果");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestSearchVariety()
        {
            Task.Run(async () =>
            {
                try
                {
                    var keyword = "大侦探";
                    var api = new MgtvApi(loggerFactory);
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    Console.WriteLine(string.Join("\n", result.Select(x => $"{x.Id} | {x.Title} | {x.TypeName} | {x.Year} | {x.VideoCount}")));

                    Assert.IsTrue(result.Count > 0, "综艺搜索应返回结果");
                    Assert.IsTrue(result.Any(x => x.TypeName == "综艺"), "应包含综艺类型的结果");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
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

                    Assert.IsNotNull(result, "视频信息不能为空");
                    Assert.IsNotNull(result.EpisodeList, "剧集列表不能为空");
                    Assert.IsTrue(result.EpisodeList.Count > 0, "剧集列表不能为空");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
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
                    var cid = "641701";
                    var vid = "20836173";
                    var api = new MgtvApi(loggerFactory);
                    var result = await api.GetDanmuContentAsync(cid, vid, CancellationToken.None);
                    Console.WriteLine(result);

                    Assert.IsNotNull(result, "弹幕列表不能为空");
                    Assert.IsTrue(result.Count > 0, "应获取到弹幕");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).GetAwaiter().GetResult();
        }

    }
}
