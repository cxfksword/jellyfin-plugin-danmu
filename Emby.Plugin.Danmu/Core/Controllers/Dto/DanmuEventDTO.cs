using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Core.Controllers.Dto
{
    public class DanmuEventDTO
    {
        /**
         * 弹幕内容
         */
        [DataMember(Name="m")]
        public string M { get; set; }
        
        /**
         * 弹幕属性
         * <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
         * time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
         * Emby.Plugin.Danmu.Scraper.Entity.ScraperDanmakuText
         */
        [DataMember(Name="p")]
        public string P { get; set; }
    }
}