using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
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

            var _bilibiliApi = new BilibiliApi(loggerFactory);
            var scraperFactory = new ScraperFactory(loggerFactory, _bilibiliApi);

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();

            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperFactory);

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

            var _bilibiliApi = new BilibiliApi(loggerFactory);
            var scraperFactory = new ScraperFactory(loggerFactory, _bilibiliApi);

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            fileSystemStub.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            fileSystemStub.Setup(x => x.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now.AddDays(-1));
            fileSystemStub.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()));
            var mediaSourceManagerStub = new Mock<IMediaSourceManager>();
            mediaSourceManagerStub.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaBrowser.Model.MediaInfo.MediaProtocol.File);
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperFactory);

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
            var _bilibiliApi = new BilibiliApi(loggerFactory);
            var scraperFactory = new ScraperFactory(loggerFactory, _bilibiliApi);

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperFactory);

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
            var _bilibiliApi = new BilibiliApi(loggerFactory);
            var scraperFactory = new ScraperFactory(loggerFactory, _bilibiliApi);

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperFactory);

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
    }
}

