
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class Episode
    {
        [DataMember(Name="episodeId")]
        public long EpisodeId { get; set; }

        [DataMember(Name="episodeTitle")]
        public string EpisodeTitle { get; set; }

        [DataMember(Name="episodeNumber")]
        public string EpisodeNumber { get; set; }
    }
}