using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentCommentResult
    {
        [DataMember(Name="segment_span")]
        public string SegmentSpan { get; set; }
        [DataMember(Name="segment_start")]
        public string SegmentStart { get; set; }

        [DataMember(Name="segment_index")]
        public Dictionary<long, TencentCommentSegment> SegmentIndex { get; set; }
    }

    public class TencentCommentSegment
    {
        [DataMember(Name="segment_name")]
        public string SegmentName { get; set; }
        [DataMember(Name="segment_start")]
        public string SegmentStart { get; set; }
    }
}