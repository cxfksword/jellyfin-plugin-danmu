using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class IqiyiTest
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
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Iqiyi(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "无间道3：终极无间",
                ProductionYear = 2003
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
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Iqiyi(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie
            {
                Name = "少年派的奇幻漂流",
                ProviderIds = new Dictionary<string, string>() { { Iqiyi.ScraperProviderId, "19rrjv4kz0" } },
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
            scraperManager.register(new Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Iqiyi(loggerFactory));

            var fileSystemStub = new Mock<Jellyfin.Plugin.Danmu.Core.IFileSystem>();
            var directoryServiceStub = new Mock<IDirectoryService>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var libraryManagerEventsHelper = new LibraryManagerEventsHelper(libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Season
            {
                Name = "沉默的真相",
                ProductionYear = 2020,
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
                    var api = new Iqiyi(loggerFactory);
                    var media = await api.GetMedia(new Season(), "19rrmacgqs");
                    Console.WriteLine(media);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }

        [TestMethod]
        public void TestGetZongyiMedia()
        {

            Task.Run(async () =>
            {
                try
                {
                    var api = new Iqiyi(loggerFactory);
                    var media = await api.GetMedia(new Season(), "1m5gylxxqu0");
                    Console.WriteLine(media);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();

        }


        [TestMethod]
        public void TestGetEpisodesForApi()
        {
            var api = new Iqiyi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var id = "o5e8yl8378"; // 综艺
                    // var id = "19tfhh8axvc"; // 电视剧
                    // var id = "1e54n0pt5ro"; // 电影
                    var result = await api.GetEpisodesForApi(id);
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
