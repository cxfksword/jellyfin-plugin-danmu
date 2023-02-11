using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity
{
    public class IqiyiCommentUser
    {
        [XmlElement("uid")]
        public string Uid { get; set; }
        [XmlElement("udid")]
        public string Udid { get; set; }

    }
}
