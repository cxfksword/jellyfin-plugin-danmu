using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core.Http
{
    public class HttpClientHandlerEx : HttpClientHandler
    {
        public HttpClientHandlerEx()
        {
            ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true; // 忽略SSL证书问题
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            CookieContainer = new CookieContainer();
            UseCookies = true; // 使用cookie
        }

        protected override Task<HttpResponseMessage> SendAsync(
       HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}
