using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiVideoListResult
    {
        [JsonPropertyName("data")]
        public IqiyiVideoListData Data { get; set; }
    }

    public class IqiyiVideoListData
    {
        [JsonPropertyName("videos")]
        public List<IqiyiVideoListInfo> Videos { get; set; }
    }

    public class IqiyiVideoListInfo
    {
        [JsonPropertyName("id")]
        public Int64 Id { get; set; }

        [JsonPropertyName("shortTitle")]
        public string ShortTitle { get; set; }

        [JsonPropertyName("publishTime")]
        public Int64 PublishTime { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("pageUrl")]
        public string PageUrl { get; set; }

        [JsonIgnore]
        public string PlayUrl
        {
            get
            {
                if (PageUrl.Contains("http"))
                {
                    return PageUrl;
                }
                else
                {
                    return $"https:{PageUrl}";
                }
            }
        }
    }

}
