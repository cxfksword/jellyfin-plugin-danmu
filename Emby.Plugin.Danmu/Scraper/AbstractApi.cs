using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Http;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper
{
    public class AbstractApi : IDisposable
    {
        public const string HTTP_USER_AGENT =
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.44";

        private const string CookieExpireReplace = @"expires=.*?;";
        
        protected ILogger _logger;
        // protected JsonSerializerOptions _jsonOptions = null;
        protected IHttpClient httpClient;
        protected CookieContainer _cookieContainer;
        protected IMemoryCache _memoryCache;

        public AbstractApi(ILogger log, IHttpClient httpClient)
        {
            this._logger = log;
            var handler = new HttpClientHandlerEx();
            _cookieContainer = handler.CookieContainer;

            this.httpClient = httpClient;
            // httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        protected virtual void AddCookies(Uri uri, string cookieVal, params char[]? separator)
        {
            // 清空旧的cookie
            // var cookies = _cookieContainer.GetCookies(uri);
            // foreach (Cookie co in cookies)
            // {
            //     co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            // }

            if (cookieVal == null)
            {
                return;
            }

            DateTime maxExpiryDate = DateTime.Now.AddMinutes(60 * 6);
            DateTime minExpiryDate = DateTime.Now.AddMinutes(10);
            string[] multCookies = separator == null || separator.Length == 0
                ? new[] { cookieVal }
                : cookieVal.Split(separator);
            _logger.Info("url={0}, set cookie = {1}, separator={2}, multCookies.length={3}", uri.AbsoluteUri, cookieVal, separator, multCookies.Length);
            foreach (string c in multCookies)
            {
                string replaceCookie = Regex.Replace(c, CookieExpireReplace, string.Empty);
                _logger.Info("one cookie={0}, noBlank={1}, url={2}", c, replaceCookie, uri.AbsoluteUri);
                CookieContainer cookieContainer = new CookieContainer();
                cookieContainer.SetCookies(uri, replaceCookie);

                CookieCollection cookieCollections = cookieContainer.GetCookies(uri);
                foreach (Cookie cookie in cookieCollections)
                {
                    if (!cookie.Expired)
                    {
                        if (cookie.Expires.CompareTo(maxExpiryDate) > 0)
                        {
                            cookie.Expires = maxExpiryDate;
                        }
                        else if (cookie.Expires.CompareTo(minExpiryDate) < 0)
                        {
                            cookie.Expires = minExpiryDate;
                        }
                    }
                }
                
                this._cookieContainer.Add(cookieCollections);
            }
        }

        protected virtual Dictionary<string, string> GetDefaultHeaders() => null;

        protected virtual string[] GetDefaultCookies(string? url = null) => null;

        protected HttpRequestOptions GetDefaultHttpRequestOptions(string url, string? cookies = null,
            CancellationToken cancellationToken = default)
        {
            HttpRequestOptions httpRequestOptions = new HttpRequestOptions
            {
                Url = url,
                UserAgent = $"{HTTP_USER_AGENT}",
                TimeoutMs = 300000,
                EnableHttpCompression = true,
                RequestContentType = "application/json",
                AcceptHeader = "application/json",
                CancellationToken = cancellationToken
            };
            
            Dictionary<string,string> requestHeaders = httpRequestOptions.RequestHeaders;
            Dictionary<string,string> defaultHeaders = GetDefaultHeaders();
            if (defaultHeaders != null )
            {
                foreach (var kvp in defaultHeaders)
                {
                    requestHeaders.Add(kvp.Key, kvp.Value);
                }
            }

            var defaultCookies = GetDefaultCookies(url);
            if (cookies != null)
            {
                requestHeaders["Cookie"] = cookies;
            }
            else if (defaultCookies != null && defaultCookies.Length > 0)
            {
                requestHeaders["Cookie"] = string.Join(",", defaultCookies);
            }

            return httpRequestOptions;
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
        
        protected virtual Task LimitRequestFrequently()
        {
            Thread.Sleep(1000);
            return Task.CompletedTask;
        }
    }
}