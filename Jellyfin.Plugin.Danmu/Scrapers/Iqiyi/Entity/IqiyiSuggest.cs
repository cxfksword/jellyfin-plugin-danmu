using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiSuggest
    {
        private static readonly Regex regLinkId = new Regex(@"(v_|a_)(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("aid")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("vid")]
        public Int64 VideoId { get; set; }
        [JsonPropertyName("link")]
        public string Link { get; set; }
        [JsonPropertyName("name")]
        public String Name { get; set; }
        [JsonPropertyName("cname")]
        public String ChannelName { get; set; }
        [JsonPropertyName("year")]
        public int Year { get; set; }

        public string LinkId
        {
            get
            {
                var match = regLinkId.Match(Link);
                if (match.Success && match.Groups.Count > 2)
                {
                    return match.Groups[2].Value.Trim();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
