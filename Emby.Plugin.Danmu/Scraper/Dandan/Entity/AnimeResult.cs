using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class AnimeResult
    {
        [DataMember(Name="errorCode")]
        public int ErrorCode { get; set; }

        [DataMember(Name="errorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name="success")]
        public bool Success { get; set; }


        [DataMember(Name="bangumi")]
        public Anime? Bangumi { get; set; }
    }
}
