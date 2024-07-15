using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class VideoSeason
    {
        [DataMember(Name="season_id")]
        public long SeasonId { get; set; }

        [DataMember(Name="title")]
        public string Title { get; set; }


        [DataMember(Name="episodes")]
        public List<VideoEpisode> Episodes { get; set; }
    }
}