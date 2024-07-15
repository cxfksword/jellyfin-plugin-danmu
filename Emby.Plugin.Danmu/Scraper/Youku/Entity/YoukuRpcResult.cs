using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Youku.Entity
{
    public class YoukuRpcResult
    {
        [DataMember(Name="data")]
        public YoukuRpcData Data { get; set; }
    }

    public class YoukuRpcData
    {
        [DataMember(Name="result")]
        public string Result { get; set; }
    }
}