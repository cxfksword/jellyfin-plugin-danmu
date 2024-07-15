using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Entity
{
    [XmlRoot("i")]
    public class ScraperDanmaku
    {
        [XmlElement("chatid")] public long ChatId { get; set; } = 0;
        [XmlElement("chatserver")] public string ChatServer { get; set; } = "chat.bilibili.com";
        
        [XmlElement("sourceprovider")] public string ProviderId { get; set; } = "DandanID";
        
        [XmlElement("datasize")] public int DataSize { get; set; } = 0;

        [XmlElement("mission")] public long Mission { get; set; } = 0;

        [XmlElement("maxlimit")] public long MaxLimit { get; set; } = 3000;
        [XmlElement("state")] public int State { get; set; } = 0;
        [XmlElement("real_name")] public int RealName { get; set; } = 0;
        [XmlElement("source")] public string Source { get; set; } = "k-v";

        [XmlElement("d")] public List<ScraperDanmakuText> Items { get; set; } = new List<ScraperDanmakuText>();

        public byte[] ToXml()
        {
            var enc = new UTF8Encoding(); // Remove utf-8 BOM

            using (MemoryStream ms = new MemoryStream())
            {
                var xmlWriterSettings = new System.Xml.XmlWriterSettings()
                {
                    // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
                    // Code analysis does not understand that. That's why there is a suppress message.
                    CloseOutput = false,
                    Encoding = enc,
                    OmitXmlDeclaration = false,
                    Indent = false
                };
                using (var xw = System.Xml.XmlWriter.Create(ms, xmlWriterSettings))
                {
                    var xmlSerializer = new XmlSerializer(typeof(ScraperDanmaku));
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    xmlSerializer.Serialize(xw, this, ns);
                }

                return ms.ToArray();
            }

            // var serializer = new XmlSerializer(typeof(ScraperDanmaku));

            // //将对象序列化输出到控制台
            // serializer.Serialize(Console.Out, cc);

            // var sb = new StringBuilder();
            // sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            // sb.Append("<i>");
            // sb.AppendFormat("<chatserver>chat.bilibili.com</chatserver><chatid>{0}</chatid><mission>0</mission><maxlimit>3000</maxlimit><state>0</state><real_name>0</real_name><source>k-v</source>", id);
            // foreach (var item in Items)
            // {
            //     // bilibili弹幕格式：
            //     // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
            //     // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
            //     var time = (Convert.ToDouble(item.Progress) / 1000).ToString("F05");
            //     sb.AppendFormat("<d p=\"{0},{1},{2},{3},{4},{5},{6},{7},{8}\">{9}</d>", time, item.Mode, item.Fontsize, item.Color, item.Ctime, item.Pool, item.MidHash, item.Id, item.Weight, item.Content);
            // }
            // sb.Append("</i>");

            // return sb.ToString();
        }
    }

    public class ScraperDanmakuText : IXmlSerializable
    {
        public long Id { get; set; } //弹幕dmID

        /// <summary>
        /// 出现时间(单位ms)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 弹幕类型 1 2 3:普通弹幕 4:底部弹幕 5:顶部弹幕 6:逆向弹幕 7:高级弹幕 8:代码弹幕 9:BAS弹幕(pool必须为2)
        /// </summary>
        public int Mode { get; set; }

        public int Fontsize { get; set; } = 25; //文字大小

        /// <summary>
        /// 弹幕颜色，默认白色
        /// </summary>
        public uint Color { get; set; } = 16777215;

        public string MidHash { get; set; } //发送者UID的HASH
        public string Content { get; set; } //弹幕内容
        public long Ctime { get; set; } //发送时间

        public int Weight { get; set; } = 1; //权重

        //public string Action { get; set; }    //动作？
        public int Pool { get; set; } //弹幕池

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            // bilibili弹幕格式：
            // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
            // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
            var time = (Convert.ToDouble(Progress) / 1000).ToString("F05");
            var attr = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", time, Mode, Fontsize, Color, Ctime, Pool, MidHash, Id, Weight);
            writer.WriteAttributeString("p", attr);
            if (IsValidXmlString(Content))
            {
                writer.WriteString(Content);
            }
            else
            {
                writer.WriteString(RemoveInvalidXmlChars(Content));
            }
        }

        private string RemoveInvalidXmlChars(string text)
        {
            var validXmlChars = text.Where(ch => XmlConvert.IsXmlChar(ch)).ToArray();
            return new string(validXmlChars);
        }

        private bool IsValidXmlString(string text)
        {
            try
            {
                XmlConvert.VerifyXmlChars(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}