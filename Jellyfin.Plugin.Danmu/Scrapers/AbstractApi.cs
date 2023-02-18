using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Core.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Scrapers;

public abstract class AbstractApi : IDisposable
{
    public const string HTTP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.44";
    protected ILogger _logger;
    protected JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    protected HttpClient httpClient;
    protected CookieContainer _cookieContainer;
    protected IMemoryCache _memoryCache;


    public AbstractApi(ILogger log)
    {
        this._logger = log;

        var handler = new HttpClientHandlerEx();
        _cookieContainer = handler.CookieContainer;
        httpClient = new HttpClient(handler, true);
        httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    protected void AddCookies(string cookieVal, Uri uri)
    {
        // 清空旧的cookie
        var cookies = _cookieContainer.GetCookies(uri);
        foreach (Cookie co in cookies)
        {
            co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
        }


        // 附加新的cookie
        if (!string.IsNullOrEmpty(cookieVal))
        {
            var domain = uri.GetSecondLevelHost();
            var arr = cookieVal.Split(';');
            foreach (var str in arr)
            {
                var cookieArr = str.Split('=');
                if (cookieArr.Length != 2)
                {
                    continue;
                }

                var key = cookieArr[0].Trim();
                var value = cookieArr[1].Trim();
                try
                {
                    _cookieContainer.Add(new Cookie(key, value, "/", "." + domain));
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, ex.Message);
                }
            }

        }

    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryCache.Dispose();
        }
    }

}