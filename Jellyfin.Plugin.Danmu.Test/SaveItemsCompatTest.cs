using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Moq;

// 模拟 JF12+ 新增接口：与真实类型同 FullName，生产代码按 FullName 反射查找，
// 因此单测程序集提供同名假接口即可验证 JF12 分支。
namespace MediaBrowser.Controller.Persistence
{
    public interface IItemPersistenceService
    {
        void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);
    }
}

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class SaveItemsCompatTest
    {
        private readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            }));

        private static Task InvokeUpdateItemsAsync(LibraryManagerEventsHelper helper, IReadOnlyList<BaseItem> items)
        {
            var method = typeof(LibraryManagerEventsHelper).GetMethod(
                "UpdateItemsAsync",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(IReadOnlyList<BaseItem>), typeof(CancellationToken) },
                null)!;
            return (Task)method.Invoke(helper, new object[] { items, CancellationToken.None })!;
        }

        [TestMethod]
        public void UpdateItemsAsync_LegacyPath_CallsItemRepositorySaveItems()
        {
            LibraryManagerEventsHelper.ResetSaveItemsLookupForTest();

            var scraperManager = new ScraperManager(loggerFactory);
            var fileSystemStub = new Mock<Core.IFileSystem>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            libraryManagerStub
                .Setup(x => x.RunMetadataSavers(It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>()))
                .Returns(Task.CompletedTask);

            // 老构造器：serviceProvider 为 null，必然走 IItemRepository 回退路径
            var helper = new LibraryManagerEventsHelper(
                itemRepositoryStub.Object, libraryManagerStub.Object, loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie { Name = "legacy", Id = Guid.NewGuid() };
            InvokeUpdateItemsAsync(helper, new[] { item }).GetAwaiter().GetResult();

            itemRepositoryStub.Verify(
                x => x.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public void UpdateItemsAsync_JF12Path_PrefersPersistenceService()
        {
            LibraryManagerEventsHelper.ResetSaveItemsLookupForTest();

            var scraperManager = new ScraperManager(loggerFactory);
            var fileSystemStub = new Mock<Core.IFileSystem>();
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            libraryManagerStub
                .Setup(x => x.RunMetadataSavers(It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>()))
                .Returns(Task.CompletedTask);

            var persistenceStub = new Mock<MediaBrowser.Controller.Persistence.IItemPersistenceService>();
            var serviceProviderStub = new Mock<IServiceProvider>();
            serviceProviderStub
                .Setup(x => x.GetService(It.Is<Type>(t => t.FullName == "MediaBrowser.Controller.Persistence.IItemPersistenceService")))
                .Returns(persistenceStub.Object);
            serviceProviderStub
                .Setup(x => x.GetService(typeof(IItemRepository)))
                .Returns(itemRepositoryStub.Object);

            var helper = new LibraryManagerEventsHelper(
                serviceProviderStub.Object, itemRepositoryStub.Object, libraryManagerStub.Object,
                loggerFactory, fileSystemStub.Object, scraperManager);

            var item = new Movie { Name = "jf12", Id = Guid.NewGuid() };
            InvokeUpdateItemsAsync(helper, new[] { item }).GetAwaiter().GetResult();

            persistenceStub.Verify(
                x => x.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()),
                Times.Once);
            itemRepositoryStub.Verify(
                x => x.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
