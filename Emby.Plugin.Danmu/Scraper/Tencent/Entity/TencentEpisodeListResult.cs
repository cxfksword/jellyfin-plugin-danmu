using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentEpisodeListResult
    {
        [DataMember(Name="data")]
        public TencentModuleDataList Data { get; set; }
    }

    public class TencentModuleDataList
    {
        [DataMember(Name="module_list_datas")]
        public List<TencentModuleList> ModuleListDatas { get; set; }
    }

    public class TencentModuleList
    {
        [DataMember(Name="module_datas")]
        public List<TencentModule> ModuleDatas { get; set; }
    }

    public class TencentModule
    {
        [DataMember(Name="item_data_lists")]
        public TencentModuleItemList ItemDataLists { get; set; }
    }

    public class TencentModuleItemList
    {
        [DataMember(Name="item_datas")]
        public List<TencentModuleItem> ItemDatas { get; set; }
    }

    public class TencentModuleItem
    {
        [DataMember(Name="item_id")]
        public string ItemId { get; set; }
        [DataMember(Name="item_type")]
        public string ItemType { get; set; }
        [DataMember(Name="item_params")]
        public TencentEpisode ItemParams { get; set; }
    }
}