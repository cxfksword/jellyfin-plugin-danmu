using System;

namespace Jellyfin.Plugin.Danmu.Core.Extensions
{
    public static class UriExtension
    {
        public static string GetSecondLevelHost(this Uri uri)
        {
            var domain = uri.Host;
            var arrHost = uri.Host.Split('.');
            if (arrHost.Length >= 2)
            {
                domain = arrHost[arrHost.Length - 2] + "." + arrHost[arrHost.Length - 1];
            }
            return domain;
        }
    }
}