using System;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class UriExtensions
    {
        public static string GetSecondLevelHost(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string host = uri.Host;
        
            // 只考虑包含点的情况，排除localhost这样的情况
            if (host.Contains("."))
            {
                // 分割域名为各个部分
                var parts = host.Split('.');
                int partsCount = parts.Length;

                // 对于常见的顶级域名或代码，比如.com, .co.uk, .gov.cn等二级域名实际上是倒数第三个部分
                // 要改进判定逻辑，您可以维护一个顶级域名列表，以便于判断二级域名的确切位置
                if (partsCount > 1)
                {
                    string topLevelDomain = parts[partsCount - 1];
                    string secondLevelDomain = parts[partsCount - 2];
                    return $"{secondLevelDomain}.{topLevelDomain}";
                }
            }
            return uri.Host; // 如果没有点，可能是localhost或类似情况，直接返回主机名
        }
    }
}