using System;
using System.Collections.Generic;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Danmu
{
    public class ServiceRegistrator : IServerEntryPoint
    {
        
        private static Dictionary<string, object> _ioc = new Dictionary<string, object>();
        
        private readonly ILogManager _logManager;
        private readonly ILogger _logger;
        // private readonly IJsonSerializer _jsonSerializer;

        // public ServiceRegistrator()
        // {
        //     _logger.Info("ServiceRegistrator 初始化");
        // }

        public ServiceRegistrator(ILogManager logManager)
        {
            _logManager = logManager;
            // _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger(this.GetType().ToString());
            _logger.Info("初始化 -- ServiceRegistrator ");
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }

        public void Run()
        {
            _logger.Info("执行自定义代码");
            // _ioc.Add(typeof(Core.IFileSystem), new Core.FileSystem());
            // _ioc.Add(typeof(ScraperManager), new ScraperManager(_logManager));
                        
            // _serviceCollection.AddSingleton<Core.IFileSystem>((ctx) =>
            // {
            //     return new Core.FileSystem();
            // });
            // _serviceCollection.AddSingleton((ctx) =>
            // {
            //     return new ScraperManager(_logManager);
            // });
            // _serviceCollection.AddSingleton((ctx) =>
            // {
            //     return new LibraryManagerEventsHelper(ctx.GetRequiredService<ILibraryManager>(), _logManager, ctx.GetRequiredService<Core.IFileSystem>(), ctx.GetRequiredService<ScraperManager>());
            // });
            _logger.Info("执行自定义代码加载完成");
        }
        //
        // public static void Register<T>(T obj)
        // {
        //     if (obj == null)
        //     {
        //         return;
        //     }
        //     
        //     _ioc.Add(obj.GetType().ToString(), obj);
        // }
        // public static void Register<T>(String name, T obj)
        // {
        //     if (obj == null)
        //     {
        //         return;
        //     }
        //     
        //     _ioc.Add(name, obj);
        // }
        //
        // public static T GetByType<T>()
        // {
        //     _ioc.TryGetValue(typeof(T).ToString(), out var service);
        //     return (T)service;
        // }

        public static T GetByType<T>(String name)
        {
            _ioc.TryGetValue(name, out var service);
            return (T)service;
        }

    }
}