using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using MediaBrowser.Common.Net;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class HttpResponseInfoExtension
    {
        public static HttpResponseInfo EnsureSuccessStatusCode(this HttpResponseInfo httpResponseInfo)
        {
            if (!httpResponseInfo.IsSuccessStatusCode())
                throw new HttpRequestException("错误码: " + httpResponseInfo.StatusCode);

            return httpResponseInfo;
        }

        public static Cookie a(this HttpResponseInfo httpResponseInfo)
        {
            // new Cookie();
            var cookieHeader = httpResponseInfo.Headers["Set-Cookie"];
            if (cookieHeader != null)
            {
                
            }

            return null;
        }

        public static bool IsSuccessStatusCode(this HttpResponseInfo httpResponseInfo)
        {
            return httpResponseInfo.StatusCode >= HttpStatusCode.OK && httpResponseInfo.StatusCode <= (HttpStatusCode) 299;
        }
    }
}