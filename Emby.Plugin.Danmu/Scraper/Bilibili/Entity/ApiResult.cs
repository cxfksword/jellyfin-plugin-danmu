using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Bilibili.Entity
{
    public class ApiResult<T>
    {
        
        [DataMember(Name="code")]
        public int Code { get; set; }

        [DataMember(Name="message")]
        public string Message { get; set; }

        [DataMember(Name="data")]
        public T Data { get; set; }

        [DataMember(Name="result")]
        public T Result { get; set; }
    }
}