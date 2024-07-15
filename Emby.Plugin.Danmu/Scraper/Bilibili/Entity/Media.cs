using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Emby.Plugin.Danmu.Core.Extensions;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class Media
    {
        static readonly Regex regHtml = new Regex(@"\<.+?\>");
        static readonly Regex regSeasonNumber = new Regex(@"第([0-9一二三四五六七八九十]+)季");

        [DataMember(Name="type")]
        public string Type { get; set; }

        [DataMember(Name="media_type")]
        public int MediaType { get; set; }
        [DataMember(Name="media_id")]
        public long MediaId { get; set; }

        [DataMember(Name="season_type")]
        public int SeasonType { get; set; }
        [DataMember(Name="season_type_name")]
        public string SeasonTypeName { get; set; }
        [DataMember(Name="ep_size")]
        public int EpisodeSize { get; set; }
        [DataMember(Name="season_id")]
        public long SeasonId { get; set; }

        [DataMember(Name="cover")]
        public string Cover { get; set; }

        [DataMember(Name="pubtime")]
        public long PublishTime { get; set; }

        private string title;
        [DataMember(Name="title")]
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