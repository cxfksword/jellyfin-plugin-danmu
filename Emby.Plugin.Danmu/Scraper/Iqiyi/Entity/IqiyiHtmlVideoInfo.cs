using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiHtmlVideoInfo
    {
        private static readonly Regex regLinkId = new Regex(@"v_(\w+?)\.html", RegexOptions.Compiled);

        [DataMember(Name="albumId")]
        public long AlbumId { get; set; }
        [DataMember(Name="tvId")]
        public long TvId { get; set; }
        [DataMember(Name="name")]
        public string VideoName { get; set; }
        [DataMember(Name="playUrl")]
        public string VideoUrl { get; set; }
        [DataMember(Name="channelId")]
        public int channelId { get; set; }
        [DataMember(Name="durationSec")]
        public int Duration { get; set; }
        [DataMember(Name="videoCount")]
        public int VideoCount { get; set; }

        [IgnoreDataMember]
        public List<IqiyiEpisode> Epsodelist { get; set; }

        [IgnoreDataMember]
        public string LinkId
        {
            get
            {
                var match = regLinkId.Match(VideoUrl);
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

        [IgnoreDataMember]
        public string ChannelName
        {
            get
            {
                switch (channelId)
                {
                    case 1:
                        return "电影";
                    case 2:
                        return "电视剧";
                    case 3:
                        return "纪录片";
                    case 4:
                        return "动漫";
                    case 6:
                        return "综艺";
                    case 15:
                        return "儿童";
                    default:
                        return string.Empty;
                }
            }
        }

    }
}