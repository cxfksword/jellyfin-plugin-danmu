using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Singleton;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class HttpClientExtension
    {
        private static readonly ILogger Logger = SingletonManager.LogManager.GetLogger("HttpClientExtension");
        private static readonly IJsonSerializer JsonSerializer = SingletonManager.JsonSerializer;
        
        public static Task<HttpResponseInfo> GetAsync(this IHttpClient httpClient,  HttpRequestOptions options)
        {
            return httpClient.GetResponse(options);
        }

        // public static async Task<object> getSelfResult<T>(this IHttpClient httpClient, string url, Func<HttpResponseInfo, T> resultGetter)
        public static async Task<T> GetSelfResultAsync<T>(this IHttpClient httpClient, HttpRequestOptions httpRequestOptions,
            Func<HttpResponseInfo, string>? resultGetter = null)
        {
            var response = await GetSelfResponse(httpClient, httpRequestOptions);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return default;
            }

            if (resultGetter != null)
            {
                string content = resultGetter.Invoke(response);
                return JsonSerializer.DeserializeFromString<T>(content);
            }
            return JsonSerializer.DeserializeFromStream<T>(response.Content);
        }
        
        public static async Task<T> GetSelfResultAsyncWithError<T>(this IHttpClient httpClient, HttpRequestOptions httpRequestOptions, Func<HttpResponseInfo, string>? resultGetter=null, string method="GET", object postData=null)
        {
            var response = await GetSelfResponse(httpClient, httpRequestOptions, method, postData);
            if (!(response.StatusCode >= HttpStatusCode.OK && response.StatusCode <= (HttpStatusCode)299))
            {
                Logger.Info("请求http异常, httpRequestOptions={0}, status={1}", httpRequestOptions.ToString(), response.StatusCode);
                throw new HttpRequestException("请求异常 code=" + response.StatusCode);
            }

            if (resultGetter != null)
            {
                string content = resultGetter.Invoke(response);
                return JsonSerializer.DeserializeFromString<T>(content);
            }
            
            return JsonSerializer.DeserializeFromStream<T>(response.Content);
        }
        
        public static async Task<T> GetSelfResultAsync<T>(this IHttpClient httpClient, HttpRequestOptions httpRequestOptions, Func<HttpResponseInfo, T> errorFunc, Func<HttpResponseInfo, string>? resultGetter)
        {
            var response = await GetSelfResponse(httpClient, httpRequestOptions).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (errorFunc != null)
                {
                    return errorFunc.Invoke(response);
                }
            }

            if (resultGetter != null)
            {
                string content = resultGetter.Invoke(response);
                return JsonSerializer.DeserializeFromString<T>(content);
            }
            return JsonSerializer.DeserializeFromStream<T>(response.Content);
        }


        public static Task<HttpResponseInfo> GetSelfResponse(this IHttpClient httpClient, HttpRequestOptions httpRequestOptions, string method="GET", object postData=null)
        {
            if (postData != null)
            {
                if (httpRequestOptions.RequestContentType.Contains("json"))
                {
                    var serializeData = JsonSerializer.SerializeToString(postData);
                    httpRequestOptions.RequestContent = new ReadOnlyMemory<char>(serializeData.ToCharArray());
                }
                else
                {
                    httpRequestOptions.SetPostData((IDictionary<string, string>)postData);
                }
            }
            if (method.Equals("POST"))
            {
                return httpClient.Post(httpRequestOptions);
            }
            
            return httpClient.GetResponse(httpRequestOptions);
        }

        private static void SetCookies(this IHttpClient httpClient, HttpRequestOptions httpRequestOptions, HttpResponseInfo httpResponseInfo)
        {
            if (httpRequestOptions == null || httpResponseInfo.Headers == null)
            {
                return;
            }

            var requestHeaders = httpRequestOptions.RequestHeaders ?? new Dictionary<string, string>();
            var cookieHeader = httpResponseInfo.Headers["Set-Cookie"];
            if (cookieHeader != null)
            {
                
            }
            
        }
    }
}