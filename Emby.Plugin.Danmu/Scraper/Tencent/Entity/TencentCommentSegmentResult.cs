using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentCommentSegmentResult
    {
        [DataMember(Name="barrage_list")]
        public List<TencentComment> BarrageList { get; set; }
    }
}