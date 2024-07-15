using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Youku.Entity
{
    public class YoukuCommentProperty
    {

        [DataMember(Name="size")]
        public int Size { get; set; }

        [DataMember(Name="color")]
        public uint Color { get; set; }

        [DataMember(Name="pos")]
        public int Pos { get; set; }

        [DataMember(Name="alpha")]
        public int Alpha { get; set; }
    }
}