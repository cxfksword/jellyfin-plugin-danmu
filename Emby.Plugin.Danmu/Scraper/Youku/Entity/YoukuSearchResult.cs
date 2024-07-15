using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Youku.Entity
{
    public class YoukuSearchResult
    {
        [DataMember(Name="pageComponentList")]
        public List<YoukuSearchComponent> PageComponentList { get; set; }
    }

    public class YoukuSearchComponent
    {
        [DataMember(Name="commonData")]
        public YoukuSearchComponentData CommonData { get; set; }
    }

    public class YoukuSearchComponentData
    {
        [DataMember(Name="showId")]
        public string ShowId { get; set; }

        [DataMember(Name="episodeTotal")]
        public int EpisodeTotal { get; set; }

        [DataMember(Name="feature")]
        public string Feature { get; set; }

        [DataMember(Name="isYouku")]
        public int IsYouku { get; set; }

        [DataMember(Name="hasYouku")]
        public int HasYouku { get; set; }

        [DataMember(Name="ugcSupply")]
        public int UgcSupply { get; set; }

        [DataMember(Name="titleDTO")]
        public YoukuSearchTitle TitleDTO { get; set; }
    }

    public class YoukuSearchTitle
    {
        [DataMember(Name="displayName")]
        public string DisplayName { get; set; }
    }
}