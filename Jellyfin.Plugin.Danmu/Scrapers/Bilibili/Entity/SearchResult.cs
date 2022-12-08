using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class SearchResult
    {
        [JsonPropertyName("result")]
        public SearchTypeResult[] Result { get; set; }
    }

    public class SearchTypeResult
    {
        [JsonPropertyName("result_type")]
        public string ResultType { get; set; }
        [JsonPropertyName("data")]
        public Media[] Data { get; set; }
    }
}
