using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core.Extensions
{
    public static class JsonExtension
    {
        public static string ToJson(this object obj)
        {
            if (obj == null) return string.Empty;

            // 不指定UnsafeRelaxedJsonEscaping，+号会被转码为unicode字符，和js/java的序列化不一致
            var jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            return JsonSerializer.Serialize(obj, jso);
        }
    }
}
