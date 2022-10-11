using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Jellyfin.Plugin.Danmu.Api.Entity
{
    public class Media
    {
        static readonly Regex regHtml = new Regex(@"\<.+?\>");

        [JsonPropertyName("media_type")]
        public int MediaType { get; set; }
        [JsonPropertyName("media_id")]
        public long MediaId { get; set; }

        [JsonPropertyName("season_type")]
        public int SeasonType { get; set; }
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
    }
}
