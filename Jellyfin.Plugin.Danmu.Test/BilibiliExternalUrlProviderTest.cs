using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili.ExternalId;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Danmu.Test;

[TestClass]
public class BilibiliExternalUrlProviderTest
{
    [TestMethod]
    public void GetExternalUrls_UsesBvidLinkForEpisodeAidAndCid()
    {
        var provider = new ExternalUrlProvider();
        var item = new Episode
        {
            ProviderIds = new Dictionary<string, string>
            {
                [Bilibili.ScraperProviderId] = "114549814468583,30084173191",
            },
        };

        var url = provider.GetExternalUrls(item).FirstOrDefault();

        Assert.AreEqual("https://www.bilibili.com/video/BV1a5J7zvEJw", url);
    }

    [TestMethod]
    public void GetExternalUrls_KeepsBangumiLinkForEpisodeWithoutComma()
    {
        var provider = new ExternalUrlProvider();
        var item = new Episode
        {
            ProviderIds = new Dictionary<string, string>
            {
                [Bilibili.ScraperProviderId] = "123456",
            },
        };

        var urls = provider.GetExternalUrls(item).ToArray();

        CollectionAssert.AreEqual(new[] { "https://www.bilibili.com/bangumi/play/ep123456" }, urls);
    }
}