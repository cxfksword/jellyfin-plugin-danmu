using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiVideoListResult
    {
        [DataMember(Name="data")]
        public IqiyiVideoListData Data { get; set; }
    }

    public class IqiyiVideoListData
    {
        [DataMember(Name="videos")]
        public List<IqiyiVideoListInfo> Videos { get; set; }
    }

    public class IqiyiVideoListInfo
    {
        [DataMember(Name="id")]
        public Int64 Id { get; set; }

        [DataMember(Name="shortTitle")]
        public string ShortTitle { get; set; }

        [DataMember(Name="publishTime")]
        public Int64 PublishTime { get; set; }

        [DataMember(Name="duration")]
        public string Duration { get; set; }

        [DataMember(Name="pageUrl")]
        public string PageUrl { get; set; }

        [IgnoreDataMember]
        public string PlayUrl
        {
            get
            {
                if (PageUrl.Contains("http"))
                {
                    return PageUrl;
                }
                else
                {
                    return $"https:{PageUrl}";
                }
            }
        }
    }
}