using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Emby.Plugin.Danmu.Scraper.Tencent.Entity
{
    public class TencentVideo
    {
        private static readonly Regex regHtml = new Regex(@"<.+?>", RegexOptions.Compiled);

        [IgnoreDataMember] public string Id { get; set; }
        [DataMember(Name="videoType")] public int VideoType { get; set; }
        [DataMember(Name="typeName")] public string TypeName { get; set; }
        private string _title = string.Empty;

        [DataMember(Name="title")]
        public string Title
        {
            get { return regHtml.Replace(_title, ""); }
            set { _title = value; }
        }

        [DataMember(Name="year")] public int? Year { get; set; }
        [DataMember(Name="subjectDoc")] public TencentSubjectDoc SubjectDoc { get; set; }
        [IgnoreDataMember] public List<TencentEpisode> EpisodeList { get; set; }
    }

    public class TencentSubjectDoc
    {
        [DataMember(Name="videoNum")] public int VideoNum { get; set; }
    }
}