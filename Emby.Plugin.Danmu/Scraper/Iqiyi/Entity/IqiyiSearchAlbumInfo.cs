using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Emby.Plugin.Danmu.Core.Extensions;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiSearchAlbumInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [DataMember(Name="albumId")]
        public Int64 AlbumId { get; set; }
        [DataMember(Name="itemTotalNumber")]
        public int ItemTotalNumber { get; set; }
        [IgnoreDataMember]
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
        [DataMember(Name="siteId")]
        public string SiteId { get; set; }
        [DataMember(Name="albumLink")]
        public string Link { get; set; }
        [DataMember(Name="videoDocType")]
        public int VideoDocType { get; set; }
        [DataMember(Name="albumTitle")]
        public String Name { get; set; }
        [DataMember(Name="channel")]
        public String Channel { get; set; }
        [IgnoreDataMember]
        public String ChannelName
        {
            get
            {
                return Channel.Split(',').FirstOrDefault() ?? string.Empty;
            }
        }
        [IgnoreDataMember]
        public int? Year
        {
            get
            {
                if (string.IsNullOrEmpty(ReleaseDate)) return null;

                return ReleaseDate.Substring(0, 4).ToInt();
            }
        }
        [DataMember(Name="releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name="videoinfos")]
        public List<IqiyiSearchVideoInfo> VideoInfos { get; set; }

        /// <summary>
        /// 编码后的视频ID
        /// </summary>
        [IgnoreDataMember]
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