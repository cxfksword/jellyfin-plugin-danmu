using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class LibraryManagerEventsHelperTest
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
              builder.AddSimpleConsole(options =>
              {
                  options.IncludeScopes = true;
                  options.SingleLine = true;
                  options.TimestampFormat = "hh:mm:ss ";
              }));

        [TestMethod]
        public void TestAddMovie()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();

            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "你的名字"
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
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            fileSystemStub.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            fileSystemStub.Setup(x => x.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now.AddDays(-1));
            fileSystemStub.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()));
            var mediaSourceManagerStub = new Mock<IMediaSourceManager>();
            mediaSourceManagerStub.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaBrowser.Model.MediaInfo.MediaProtocol.File);
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "四海",
                ProviderIds = new Dictionary<string, string>() { { "BilibiliID", "BV1Sx411c7wA" } },
                Path = "/tmp/test.mp4",
            };
            Movie.MediaSourceManager = mediaSourceManagerStub.Object;

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
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "奔跑吧兄弟"
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
        public void TestUpdateSeason()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "奔跑吧兄弟",
                ProviderIds = new Dictionary<string, string>() { { "BilibiliID", "33930" } }
            };

            var list = new List<LibraryEvent>();
            list.Add(new LibraryEvent { Item = item, EventType = EventType.Update });

            Task.Run(async () =>
            {
                try
                {
                    await libraryManagerEventsHelper.ProcessQueuedSeasonEvents(list, EventType.Update);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }


        [TestMethod]
        public void TestAddMovieByDandan()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Dandan(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();

            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "你的名字"
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
        public void TestAddSeasonByDandan()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Dandan(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();

            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "混沌武士"
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
        public void TestAddMovieByMultiScrapers()
        {
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(loggerFactory));
            scraperManager.Register(new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Dandan(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();

            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "你的名字"
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
        public void TestDownloadDanmu()
        {
            var dandanScraper = new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Dandan(loggerFactory);
            var scraperManager = new ScraperManager(loggerFactory);
            scraperManager.Register(dandanScraper);

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            fileSystemStub.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            fileSystemStub.Setup(x => x.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now.AddDays(-1));
            fileSystemStub.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()));
            var mediaSourceManagerStub = new Mock<IMediaSourceManager>();
            mediaSourceManagerStub.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaBrowser.Model.MediaInfo.MediaProtocol.File);
            var directoryServiceStub = new Mock<IDirectoryService>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "阿基拉",
                ProviderIds = new Dictionary<string, string>() { { "DandanID", "280001" } },
                Path = "/tmp/test.mp4",
            };
            Movie.MediaSourceManager = mediaSourceManagerStub.Object;

            var list = new List<LibraryEvent>();
            list.Add(new LibraryEvent { Item = item, EventType = EventType.Add });

            Task.Run(async () =>
            {
                try
                {
                    await libraryManagerEventsHelper.DownloadDanmu(dandanScraper, item, "280001");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }


    }
}

