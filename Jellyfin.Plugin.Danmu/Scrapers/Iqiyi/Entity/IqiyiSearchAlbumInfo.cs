using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiSearchAlbumInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [JsonPropertyName("albumId")]
        public Int64 AlbumId { get; set; }
        [JsonPropertyName("itemTotalNumber")]
        public int ItemTotalNumber { get; set; }
        [JsonIgnore]
        public Int64 VideoId
        {
            get
            {
                if (VideoInfos != null)
                {
                    return VideoInfos.FirstOrDefault()?.VideoId ?? 0;
                }
                else
                {
                    return 0;
                }
            }
        }
        [JsonPropertyName("siteId")]
        public string SiteId { get; set; }
        [JsonPropertyName("albumLink")]
        public string Link { get; set; }
        [JsonPropertyName("videoDocType")]
        public int VideoDocType { get; set; }
        [JsonPropertyName("albumTitle")]
        public String Name { get; set; }
        [JsonPropertyName("channel")]
        public String Channel { get; set; }
        [JsonIgnore]
        public String ChannelName
        {
            get
            {
                return Channel.Split(",").FirstOrDefault() ?? string.Empty;
            }
        }
        [JsonIgnore]
        public int? Year
        {
            get
            {
                if (string.IsNullOrEmpty(ReleaseDate)) return null;

                return ReleaseDate.Substring(0, 4).ToInt();
            }
        }
        [JsonPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }

        [JsonPropertyName("videoinfos")]
        public List<IqiyiSearchVideoInfo> VideoInfos { get; set; }

        /// <summary>
        /// 编码后的视频ID
        /// </summary>
        [JsonIgnore]
        public string? LinkId
        {
            get
            {
                var link = Link;
                if (VideoInfos != null && VideoInfos.Count > 0)
                {
                    link = VideoInfos.First().ItemLink;
                }

                var match = regLinkId.Match(link);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
