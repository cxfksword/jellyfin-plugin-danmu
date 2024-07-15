using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentSearchRequest
    {
        [DataMember(Name="version")]
        public string Version { get; set; } = string.Empty;
        [DataMember(Name="filterValue")]
        public string FilterValue { get; set; } = "firstTabid=150";
        [DataMember(Name="retry")]
        public int Retry { get; set; } = 0;
        [DataMember(Name="query")]
        public string Query { get; set; }
        [DataMember(Name="pagenum")]
        public int PageNum { get; set; } = 0;
        [DataMember(Name="pagesize")]
        public int PageSize { get; set; } = 20;
        [DataMember(Name="queryFrom")]
        public int QueryFrom { get; set; } = 4;
        [DataMember(Name="isneedQc")]
        public bool IsneedQc { get; set; } = true;
        [DataMember(Name="adRequestInfo")]
        public string AdRequestInfo { get; set; } = string.Empty;
        [DataMember(Name="sdkRequestInfo")]
        public string SdkRequestInfo { get; set; } = string.Empty;
        [DataMember(Name="sceneId")]
        public int SceneId { get; set; } = 21;
        [DataMember(Name="platform")]
        public string Platform { get; set; } = "23";
    }
}