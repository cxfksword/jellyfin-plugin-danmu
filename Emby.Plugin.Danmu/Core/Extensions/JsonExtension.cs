using System;
using System.IO;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Singleton;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class JsonExtension
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            IncludeNullValues=true,
            Indent = true,
            ExcludeTypeInfo = false
        };

        private static readonly IJsonSerializer JsonSerializer = SingletonManager.JsonSerializer;
        
        public static string ToJson(this object obj)
        {
            if (obj == null) return string.Empty;
            // 不指定UnsafeRelaxedJsonEscaping，+号会被转码为unicode字符，和js/java的序列化不一致
            
            return SingletonManager.JsonSerializer.SerializeToString(obj, JsonSerializerOptions);
            // return JsonConvert.SerializeObject(obj);
        }

        public static Task<T> ReadFromJsonAsync<T>(this Stream content)
        {
            return JsonSerializer.DeserializeFromStreamAsync<T>(content);
        }

        public static T FromJson<T>(this string str)
        {
            if (string.IsNullOrEmpty(str)) return default(T);

            // 不指定UnsafeRelaxedJsonEscaping，+号会被转码为unicode字符，和js/java的序列化不一致
            var jso = new JsonSerializerOptions();
            try
            {
                return JsonSerializer.DeserializeFromString<T>(str);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}