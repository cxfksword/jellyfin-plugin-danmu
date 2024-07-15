using System;
using System.Linq;
using MediaBrowser.Model.Logging;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class ILoggerExtension
    {
        private static readonly string DefaultName = "com.self.danmu.";
        public static ILogger getDefaultLogger(this ILogManager logManager, params string?[] args)
        {
            string logName = string.Join(".", args.Where(arg => !string.IsNullOrEmpty(arg)).Select(arg => $"{DefaultName}{arg}"));
            return logManager.GetLogger(logName);
        }

        public static void LogError(this ILogger logger, Exception? ex, string? message, params object?[] args)
        {
            logger.ErrorException(message, ex, args);
        }

        public static void LogInformation(this ILogger logger, string? message, params object?[] args)
        {
            logger.Info(message, args);
        }

        public static void LogDebug(this ILogger logger, string? message, params object?[] args)
        {
            logger.Debug(message, args);
        }
    }
}