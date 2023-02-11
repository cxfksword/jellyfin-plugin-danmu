using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
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
