using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi.Entity
{
    public class IqiyiAlbum
    {
        private static readonly Regex regLinkId = new Regex(@"(v_|a_)(\w+?)\.html", RegexOptions.Compiled);

        [DataMember(Name="albumId")]
        public Int64 AlbumId { get; set; }
        [DataMember(Name="url")]
        public string Url { get; set; }
        [DataMember(Name="name")]
        public String Name { get; set; }

        [DataMember(Name="firstVideo")]
        public IqiyiVideo FirstVideo { get; set; }

        [DataMember(Name="latestVideo")]
        public IqiyiVideo LatestVideo { get; set; }
    }
}