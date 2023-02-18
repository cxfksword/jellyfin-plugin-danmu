using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class TencentTest : BaseTest
    {
        [TestMethod]
        public void TestAddMovie()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Tencent.Tencent(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "流浪地球"
            };

            var list = new List<LibraryEvent>();
            list.Add(new LibraryEvent { Item = item, EventType = EventType.Add });

            Task.Run(async () =>
            {
                try
                {
                    await libraryManagerEventsHelper.ProcessQueuedMovieEvents(list, EventType.Add);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }


        [TestMethod]
        public void TestUpdateMovie()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Tencent.Tencent(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "少年派的奇幻漂流",
                ProviderIds = new Dictionary<string, string>() { { Tencent.ScraperProviderId, "19rrjv4kz0" } },
            };

            var list = new List<LibraryEvent>();
            list.Add(new LibraryEvent { Item = item, EventType = EventType.Update });

            Task.Run(async () =>
            {
                try
                {
                    await libraryManagerEventsHelper.ProcessQueuedMovieEvents(list, EventType.Update);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }




        [TestMethod]
        public void TestAddSeason()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Tencent.Tencent(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "三体",
                ProductionYear = 2023,
            };

            var list = new List<LibraryEvent>();
            list.Add(new LibraryEvent { Item = item, EventType = EventType.Add });

            Task.Run(async () =>
            {
                try
                {
                    await libraryManagerEventsHelper.ProcessQueuedSeasonEvents(list, EventType.Add);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }

        [TestMethod]
        public void TestGetMedia()
        {

            Task.Run(async () =>
            {
                try
                {
                    var api = new Tencent(loggerFactory);
                    var media = await api.GetMedia(new Season(), "mzc002006n62s11");
                    Console.WriteLine(media);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }

        [TestMethod]
        public void TestGetDanmuContent()
        {

            Task.Run(async () =>
            {
                try
                {
                    var api = new Tencent(loggerFactory);
                    var media = await api.GetDanmuContent(new Movie(), "c0034tn5bx8");
                    Console.WriteLine(media);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }
    }
}
