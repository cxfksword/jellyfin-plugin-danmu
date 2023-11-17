using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity
{
    public class Media
    {
        static readonly Regex regHtml = new Regex(@"\<.+?\>");
        static readonly Regex regSeasonNumber = new Regex(@"第([0-9一二三四五六七八九十]+)季");

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("media_type")]
        public int MediaType { get; set; }
        [JsonPropertyName("media_id")]
        public long MediaId { get; set; }

        [JsonPropertyName("season_type")]
        public int SeasonType { get; set; }
        [JsonPropertyName("season_type_name")]
        public string SeasonTypeName { get; set; }
        [JsonPropertyName("ep_size")]
        public int EpisodeSize { get; set; }
        [JsonPropertyName("season_id")]
        public long SeasonId { get; set; }

        [JsonPropertyName("cover")]
        public string Cover { get; set; }

        [JsonPropertyName("pubtime")]
        public long PublishTime { get; set; }

        private string title;
        [JsonPropertyName("title")]
        public string Title
        {
            get
            {
                return regHtml.Replace(title, "");
            }
            set
            {
                title = value;
            }
        }


        [JsonIgnore]
        public int SeasonNumber {
            get {
                var number = regSeasonNumber.FirstMatchGroup(title);
                
                // 替换中文数字为阿拉伯数字
                return number.Replace("一", "1").Replace("二", "2").Replace("三", "3").Replace("四", "4").Replace("五", "5").Replace("六", "6").Replace("七", "7").Replace("八", "8").Replace("九", "9").ToInt();
            }
        }
    }
}
