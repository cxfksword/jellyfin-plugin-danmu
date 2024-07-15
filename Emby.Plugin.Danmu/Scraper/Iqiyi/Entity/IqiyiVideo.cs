using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiVideo
    {
        [DataMember(Name="albumId")]
        public Int64 AlbumId { get; set; }
        [DataMember(Name="tvId")]
        public Int64 TvId { get; set; }
        [DataMember(Name="playUrl")]
        public string PlayUrl { get; set; }
        [DataMember(Name="albumUrl")]
        public string AlbumUrl { get; set; }
        [DataMember(Name="name")]
        public String Name { get; set; }
        [DataMember(Name="channelId")]
        public int ChannelId { get; set; }
        [DataMember(Name="videoCount")]
        public int VideoCount { get; set; }
        [DataMember(Name="duration")]
        public String Duration { get; set; }
        [DataMember(Name="publishTime")]
        public Int64 publishTime { get; set; }

        [DataMember(Name="epsodelist")]
        public List<IqiyiEpisode> Epsodelist { get; set; }
    }
}