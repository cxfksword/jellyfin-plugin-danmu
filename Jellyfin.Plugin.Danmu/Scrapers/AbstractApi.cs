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
    public class HttpConfig
    {
        /// <summary>
        /// 代理服务器地址（可选）
        /// </summary>
        public string Proxy { get; set; }

        /// <summary>
        /// User-Agent 头（可选）
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Cookie 信息（可选）
        /// </summary>
        public string Cookie { get; set; }

        /// <summary>
        /// 默认构造函数，初始化为空
        /// </summary>
        public HttpConfig()
        {
            Proxy = string.Empty;
            UserAgent = string.Empty;
            Cookie = string.Empty;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public HttpConfig(string proxy, string userAgent, string cookie)
        {
            Proxy = proxy;
            UserAgent = userAgent;
            Cookie = cookie;
        }
    }

    public const string HTTP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.44";
    protected ILogger _logger;
    protected JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    protected HttpClient httpClient;
    protected CookieContainer _cookieContainer;
    protected IMemoryCache _memoryCache;
    protected HttpConfig _httpConfig;
    protected HttpClientHandler _handler;

    public AbstractApi(ILogger log)
    {
        this._logger = log;

        _handler = new HttpClientHandlerEx();
        _cookieContainer = _handler.CookieContainer;
        httpClient = new HttpClient(_handler, true);
        httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }
    
    /// <summary>
    /// 从前端配置获取HTTP相关设置并更新到该API实例中
    /// </summary>
    public virtual void Configure(HttpConfig config)
    {
        _httpConfig = config;
        TryConfigureHttpProxy(config);
        TryConfigureUserAgent(config);
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

    /// <summary>
    /// 尝试配置HTTP代理
    /// </summary>
    protected void TryConfigureHttpProxy(HttpConfig config)
    {
        if (config == null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(config.Proxy))
        {
            _handler.Proxy = new WebProxy(config.Proxy);
            _handler.UseProxy = true;
        }
    }

    /// <summary>
    /// 尝试配置User-Agent头
    /// </summary>
    protected void TryConfigureUserAgent(HttpConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(config.UserAgent))
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
        }
    }

    /// <summary>
    /// 尝试配置Cookie信息
    /// </summary>
    protected void TryConfigureCookie(HttpConfig config, Uri cookieUri)
    {
        if (config == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(config.Cookie))
        {
            AddCookies(config.Cookie, cookieUri);
        }
    }

}