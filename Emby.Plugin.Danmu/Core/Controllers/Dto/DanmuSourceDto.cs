using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Core.Controllers.Dto
{
    public class DanmuSourceDto
    {
        [DataMember(Name="source")] public string Source { get; set; }
        
        [DataMember(Name="sourceName")] public string SourceName { get; set; }
        
        [DataMember(Name="opened")] public bool Opened { get; set; }

        /**
         * 
         */
        [DataMember(Name="danmuEvents")]
        public List<DanmuEventDTO> DanmuEvents { get; set; }
    }
}