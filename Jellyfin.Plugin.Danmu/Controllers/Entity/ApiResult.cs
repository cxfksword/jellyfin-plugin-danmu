using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Controllers.Entity
{
    public class ApiResult<T>
    {
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; } = 0;

        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
        [JsonPropertyName("animes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<T>? Animes { get; set; }
        [JsonPropertyName("bangumi")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Bangumi { get; set; } = default(T);

        public ApiResult()
        {
        }
    }
}