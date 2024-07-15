using System.Collections.Generic;

namespace Emby.Plugin.Danmu.Scraper.Entity
{
    public class ScraperMedia
    {
        /// <summary>
        /// item是电影/季时，使用本id作为元数据值
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// item是季时，本CommentId用不到
        /// </summary>
        public string CommentId { get; set; }
        public List<ScraperEpisode> Episodes { get; set; } = new List<ScraperEpisode>();

    }
    
    public class ScraperEpisode
    {
        /// <summary>
        /// 当item是剧集时，使用本id作为元数据值
        /// </summary>
        public string Id { get; set; }
        public string CommentId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}