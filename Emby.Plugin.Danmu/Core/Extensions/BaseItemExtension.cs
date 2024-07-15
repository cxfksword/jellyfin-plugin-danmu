using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Singleton;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Danmu.Core.Extensions
{
    public static class BaseItemExtension
    {
        public static Task UpdateToRepositoryAsync(this BaseItem item, ItemUpdateType itemUpdateType,
            CancellationToken cancellationToken)
        {
            item.UpdateToRepository(ItemUpdateType.MetadataEdit);
            return Task.CompletedTask;
        }

        public static string GetDanmuXmlPath(this BaseItem item, string providerId)
        {
            return item.FileNameWithoutExtension + "_" + providerId + ".xml";
        }

        /**
         * 获取弹幕id
         */
        public static string GetDanmuProviderId(this BaseItem item, string providerId)
        {
            string providerVal = item.GetProviderId(providerId);
            if (!string.IsNullOrEmpty(providerVal))
            {
                return providerVal;
            }

            if (item is Season)
            {   
                return item.GetParent().GetProviderId(providerId);    
            }
            return providerVal;
        }

        /**
         * season 获取id问题，可能存在没有season的问题，需要使用SeriesId
         */
        public static Guid GetSeasonId(this Season season)
        {
            Guid seasonId = season.Id;
            if (!Guid.Empty.Equals(seasonId))
            {
                return seasonId;
            }

            return season.GetParent().Id;
        }

        /**
         * 是否存在相应的id
         */
        public static bool HasAnyDanmuProviderIds(this BaseItem item)
        {
            var scrapers = SingletonManager.ScraperManager.All();
            if (scrapers == null || scrapers.Count == 0)
            {
                return false;
            }

            foreach (var scraper in scrapers)
            {
                if (item.HasProviderId(scraper.ProviderId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}