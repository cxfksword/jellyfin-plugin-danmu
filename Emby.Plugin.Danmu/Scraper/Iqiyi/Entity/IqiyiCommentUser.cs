using System.Xml.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiCommentUser
    {
        [XmlElement("uid")]
        public string Uid { get; set; }
        [XmlElement("udid")]
        public string Udid { get; set; }
    }
}