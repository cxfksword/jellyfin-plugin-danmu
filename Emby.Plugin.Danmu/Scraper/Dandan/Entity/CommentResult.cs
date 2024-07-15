using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class CommentResult
    {
        [DataMember(Name="comments")]
        public List<Comment> Comments { get; set; }
    }
}
