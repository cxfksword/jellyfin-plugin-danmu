using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentEpisodeListRequest
    {
        [DataMember(Name="page_params")]
        public TencentPageParams PageParams { get; set; }
    }

    public class TencentPageParams
    {
        [DataMember(Name="page_type")]
        public string PageType { get; set; } = "detail_operation";
        [DataMember(Name="page_id")]
        public string PageId { get; set; } = "vsite_episode_list";
        [DataMember(Name="id_type")]
        public string IdType { get; set; } = "1";
        [DataMember(Name="page_size")]
        public string PageSize { get; set; } = "100";
        [DataMember(Name="cid")]
        public string Cid { get; set; }
        [DataMember(Name="lid")]
        public string Lid { get; set; } = "0";
        [DataMember(Name="req_from")]
        public string ReqFrom { get; set; } = "web_mobile";
        [DataMember(Name="page_context")]
        public string PageContext { get; set; } = string.Empty;
    }
}