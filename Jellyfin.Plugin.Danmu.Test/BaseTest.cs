using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Configuration;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class BaseTest
    {
        protected ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
             builder.AddSimpleConsole(options =>
             {
                 options.IncludeScopes = true;
                 options.SingleLine = true;
                 options.TimestampFormat = "hh:mm:ss ";
             }));

        protected Plugin? mockPlugin;
        protected PluginConfiguration mockConfiguration = new PluginConfiguration();

        [TestInitialize]
        public void SetUp()
        {
            DotNetEnv.Env.TraversePath().Load();

            // Mock Plugin.Instance 及其依赖项
            var mockApplicationPaths = new Mock<IApplicationPaths>();
            mockApplicationPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
            mockApplicationPaths.Setup(p => p.PluginsPath).Returns(Path.GetTempPath());
            
            var mockApplicationHost = new Mock<IApplicationHost>();
            mockApplicationHost.Setup(h => h.GetExports<Scrapers.AbstractScraper>(false))
                .Returns(new List<Scrapers.AbstractScraper>());
            
            var mockXmlSerializer = new Mock<IXmlSerializer>();
            
            var mockScraperManager = new Mock<Scrapers.ScraperManager>(
                Mock.Of<ILoggerFactory>());

            try
            {
                // 创建 Plugin 实例
                mockPlugin = new Plugin(
                    mockApplicationPaths.Object,
                    mockApplicationHost.Object,
                    mockXmlSerializer.Object,
                    mockScraperManager.Object
                );

                // 使用反射设置 Configuration
                var configField = typeof(Plugin).BaseType?
                    .GetField("_configuration", BindingFlags.NonPublic | BindingFlags.Instance);
                if (configField != null)
                {
                    configField.SetValue(mockPlugin, mockConfiguration);
                }
            }
            catch
            {
                // 如果无法创建 Plugin 实例，使用反射直接设置静态实例
                // 这是一个后备方案
            }
        }

        [TestCleanup]
        public void TearDown()
        {
            // 清理 Plugin.Instance
            var instanceProperty = typeof(Plugin).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            instanceProperty?.SetValue(null, null);

            // 清理代码
            // 例如，释放资源或重置状态
        }
    }
}