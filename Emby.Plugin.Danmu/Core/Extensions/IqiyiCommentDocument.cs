using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Emby.Plugin.Danmu.Scraper.Iqiyi.Entity;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    [XmlRoot("danmu")]
    public class IqiyiCommentDocument
    {
        [XmlElement("sum")]
        public Int64 sum { get; set; }

        [XmlElement("validSum")]
        public Int64 validSum { get; set; }

        [XmlElement("duration")]
        public Int64 duration { get; set; }


        [XmlArray("data")]
        [XmlArrayItem("entry", typeof(IqiyiCommentEntry))]
        public List<IqiyiCommentEntry> Data { get; set; }
    }


    public class IqiyiCommentEntry
    {
        [XmlElement("int")]
        public int Index { get; set; }

        [XmlArray("list")]
        [XmlArrayItem("bulletInfo", typeof(IqiyiComment))]
        public List<IqiyiComment> List { get; set; }
    }
}