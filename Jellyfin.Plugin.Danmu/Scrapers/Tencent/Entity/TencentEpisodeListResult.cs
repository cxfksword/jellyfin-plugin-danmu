using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;

public class TencentEpisodeListResult
{
    [JsonPropertyName("data")]
    public TencentModuleDataList Data { get; set; }
}

public class TencentModuleDataList
{
    [JsonPropertyName("module_list_datas")]
    public List<TencentModuleList> ModuleListDatas { get; set; }
}

public class TencentModuleList
{
    [JsonPropertyName("module_datas")]
    public List<TencentModule> ModuleDatas { get; set; }
}

public class TencentModule
{
    [JsonPropertyName("item_data_lists")]
    public TencentModuleItemList ItemDataLists { get; set; }
}

public class TencentModuleItemList
{
    [JsonPropertyName("item_datas")]
    public List<TencentModuleItem> ItemDatas { get; set; }
}

public class TencentModuleItem
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; }
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; }
    [JsonPropertyName("item_params")]
    public TencentEpisode ItemParams { get; set; }
}