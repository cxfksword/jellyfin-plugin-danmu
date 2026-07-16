using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class BilibiliTest
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
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "冰雪奇缘",
                ProductionYear = 2013,
            };

            Task.Run(async () =>
            {
                try
                {
                    var scraper = new Bilibili(loggerFactory);
                    var result = await scraper.Search(item);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }

        [TestMethod]
        public void TestSearchMediaId()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            // var item = new Season
            // {
            //     Name = "孤独的美食家",
            //     IndexNumber = 2,
            // };

            var item = new Movie
            {
                Name = "冰雪奇缘[国粤英多音轨+简繁英双语特效字幕].Frozen.2013.UHD.BluRay.2160p.TrueHD7.1.4Audio.Atmos.x265.10bit-DreamHD",
                ProductionYear = 2013,
            };

            Task.Run(async () =>
            {
                try
                {
                    var scraper = new Bilibili(loggerFactory);
                    var result = await scraper.SearchMediaId(item);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }


        [TestMethod]
        public void TestAddMovie()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "扬名立万"
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
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "四渡赤水",
                ProviderIds = new Dictionary<string, string>() { { DanmuProviderId.UnifiedProviderId, "bilibili:BV1huGizKEZd" } },
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
        public void TestUpdateMovieWithAvid()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "新龙门客栈",
                ProviderIds = new Dictionary<string, string>() { { Bilibili.ScraperProviderId, "av5921024" } },
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
            scraperManager.Register(new Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<MediaBrowser.Controller.Persistence.IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "逃避虽可耻但有用",
                ProductionYear = 2016,
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
    }
}
