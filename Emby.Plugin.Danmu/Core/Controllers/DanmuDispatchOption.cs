namespace Emby.Plugin.Danmu.Core.Controllers
{
    public class DanmuDispatchOption
    {
        /**
         * 获取对应id的json弹幕信息
         */
        public static string GetJsonById = "GetJsonById";
        
        /**
         * 获取支持的全部站点信息
         */
        public static string GetAllSupportSite = "GetAllSupportSite";
        
        /**
         * 刷新某个id
         */
        public static string Refresh = "Refresh";
        
        /**
         * 查询某个弹幕
         */
        public static string SearchDanmu = "SearchDanmu";
    }
}