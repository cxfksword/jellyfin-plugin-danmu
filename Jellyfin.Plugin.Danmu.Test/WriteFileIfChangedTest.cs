using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class WriteFileIfChangedTest
    {
        private readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            }));

        private static LibraryManagerEventsHelper CreateHelper(Core.IFileSystem fileSystem)
        {
            var scraperManager = new ScraperManager(LoggerFactory.Create(b => { }));
            var itemRepositoryStub = new Mock<IItemRepository>();
            var libraryManagerStub = new Mock<ILibraryManager>();
            return new LibraryManagerEventsHelper(
                itemRepositoryStub.Object, libraryManagerStub.Object,
                LoggerFactory.Create(b => { }), fileSystem, scraperManager);
        }

        private static Task InvokeWriteFileIfChangedAsync(LibraryManagerEventsHelper helper, string path, byte[] bytes)
        {
            var method = typeof(LibraryManagerEventsHelper).GetMethod(
                "WriteFileIfChangedAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task)method.Invoke(helper, new object[] { path, bytes })!;
        }

        [TestMethod]
        public async Task WriteFileIfChangedAsync_UsesSystemTempDir()
        {
            string? capturedTmp = null;
            var fileSystemStub = new Mock<Core.IFileSystem>();
            fileSystemStub.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
            fileSystemStub
                .Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, byte[], CancellationToken>((p, b, ct) => capturedTmp = p)
                .Returns<string, byte[], CancellationToken>((p, b, ct) => File.WriteAllBytesAsync(p, b, ct));

            var helper = CreateHelper(fileSystemStub.Object);

            var destDir = Path.Combine(Path.GetTempPath(), "danmu-dest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, "1.xml");
            try
            {
                var bytes = new byte[] { 1, 2, 3, 4 };
                await InvokeWriteFileIfChangedAsync(helper, dest, bytes);

                Assert.IsNotNull(capturedTmp, "应经过临时文件写入");
                var tmpDir = Path.GetDirectoryName(capturedTmp)!;
                var systemTmp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
                Assert.AreEqual(
                    systemTmp,
                    tmpDir.TrimEnd(Path.DirectorySeparatorChar),
                    "临时文件应在系统临时目录，而非影片目录");
                Assert.IsFalse(
                    capturedTmp!.StartsWith(destDir, StringComparison.Ordinal),
                    "临时文件不应落在影片目录，避免触发媒体库扫描");

                CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(dest));
                Assert.AreEqual(0, Directory.GetFiles(destDir, "*.tmp").Length, "影片目录不应残留 .tmp");
            }
            finally
            {
                try { Directory.Delete(destDir, true); } catch { }
                try { if (capturedTmp != null && File.Exists(capturedTmp)) File.Delete(capturedTmp); } catch { }
            }
        }

        [TestMethod]
        public async Task WriteFileIfChangedAsync_SkipsWhenUnchanged()
        {
            var destDir = Path.Combine(Path.GetTempPath(), "danmu-dest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, "1.xml");
            var bytes = new byte[] { 5, 6, 7 };
            await File.WriteAllBytesAsync(dest, bytes);
            try
            {
                var fileSystemStub = new Mock<Core.IFileSystem>();
                fileSystemStub.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

                var helper = CreateHelper(fileSystemStub.Object);
                await InvokeWriteFileIfChangedAsync(helper, dest, bytes);

                fileSystemStub.Verify(
                    x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                    Times.Never,
                    "内容无变化时不应写盘");
            }
            finally
            {
                try { Directory.Delete(destDir, true); } catch { }
            }
        }
    }
}
