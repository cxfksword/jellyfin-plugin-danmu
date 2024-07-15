using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Danmu.Scraper.Dandan.Entity
{
    public class Anime
    {
        [DataMember(Name="animeId")]
        public long AnimeId { get; set; }

        [DataMember(Name="animeTitle")]
        public string AnimeTitle { get; set; }

        [DataMember(Name="type")]
        public string Type { get; set; }

        [DataMember(Name="typeDescription")]
        public string TypeDescription { get; set; }

        [DataMember(Name="imageUrl")]
        public string ImageUrl { get; set; }

        [DataMember(Name="startDate")]
        public string? StartDate { get; set; }

        [DataMember(Name="episodeCount")]
        public int? EpisodeCount { get; set; }

        [DataMember(Name="episodes")]
        public List<Episode>? Episodes { get; set; }

        public int? Year
        {
            get
            {
                try
                {
                    if (StartDate == null)
                    {
                        return null;
                    }

                    return DateTime.Parse(StartDate).Year;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}