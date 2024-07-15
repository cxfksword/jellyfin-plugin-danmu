using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Youku.Entity
{
    public class YoukuVideo
    {
        [DataMember(Name="total")]
        public int Total { get; set; }

        [DataMember(Name="videos")]
        public List<YoukuEpisode> Videos { get; set; } = new List<YoukuEpisode>();

        [IgnoreDataMember]
        public string ID { get; set; }

        [IgnoreDataMember]
        public string Title { get; set; }

        [IgnoreDataMember]
        public int? Year { get; set; }

        [IgnoreDataMember]
        public string Type { get; set; }

    }
}
