using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Youku.Entity
{
    public class YoukuCommentResult
    {
        [DataMember(Name="data")]
        public YoukuCommentData Data { get; set; }
    }

    public class YoukuCommentData
    {
        [DataMember(Name="result")]
        public List<YoukuComment> Result { get; set; }
    }

}