using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class SearchResult
    {
        [DataMember(Name="errorCode")]
        public int ErrorCode { get; set; }

        [DataMember(Name="errorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name="success")]
        public bool Success { get; set; }



        [DataMember(Name="animes")]
        public List<Anime> Animes { get; set; }
    }
}