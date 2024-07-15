using System;
using System.Xml.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiComment
    {
        [XmlElement("contentId")]
        public string ContentId { get; set; }

        [XmlElement("content")]
        public string Content { get; set; }

        [XmlElement("font")]
        public int Font { get; set; }

        [XmlElement("color")]
        public string Color { get; set; }
        [XmlElement("userInfo")]
        public IqiyiCommentUser UserInfo { get; set; }
        [XmlElement("showTime")]
        public Int64 ShowTime { get; set; }
    }
}