using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class MgtvTest : BaseTest
    {
        [TestMethod]
        public void TestAddMovie()
        {
            var libraryManagerStub = new Mock<ILibraryManager>();
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Mgtv(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();

            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "虚颜"
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
            var libraryManagerStub = new Mock<ILibraryManager>();
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Mgtv(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "虚颜",
                ProviderIds = new Dictionary<string, string>() { { Mgtv.ScraperProviderId, "519236" } },
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
            var libraryManagerStub = new Mock<ILibraryManager>();
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Mgtv(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "大侦探 第八季",
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
                    var libraryManagerStub = new Mock<ILibraryManager>();
                    var api = new Mgtv(loggerFactory);
                    var media = await api.GetMedia(new Season(), "514446");
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
