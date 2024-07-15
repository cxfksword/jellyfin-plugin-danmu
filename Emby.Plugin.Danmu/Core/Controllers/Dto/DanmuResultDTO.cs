using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Core.Controllers.Dto
{
    public class DanmuResultDto
    {
        [DataMember(Name="hasNext")]
        public bool HasNext
        {
            get => false;
            set { }
        }

        [DataMember(Name="data")]
        public List<DanmuSourceDto> Data { get; set; }

        [DataMember(Name="extra")]
        public string Extra { get; set; }
    }
}