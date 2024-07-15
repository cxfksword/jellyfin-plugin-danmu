
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Model
{
    public class DanmuFileInfo
    {
        [DataMember(Name = "url")]
        public string Url { get; set; } = string.Empty;

    }
}