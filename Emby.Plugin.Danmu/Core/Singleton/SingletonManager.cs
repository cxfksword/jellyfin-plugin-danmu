using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Danmu.Core.Singleton
{
    public static class SingletonManager
    {
        public static bool IsDebug = false;
        
        public static ScraperManager ScraperManager;
        public static IJsonSerializer JsonSerializer;
        public static LibraryManagerEventsHelper LibraryManagerEventsHelper;
        public static ILogManager LogManager;
        public static IHttpClient HttpClient;
        public static IApplicationHost applicationHost;
    }
}