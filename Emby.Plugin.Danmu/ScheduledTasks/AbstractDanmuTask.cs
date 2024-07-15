using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;

namespace Emby.Plugin.Danmu.ScheduledTasks
{
    public abstract class AbstractDanmuTask : IScheduledTask
    {
        public abstract Task Execute(CancellationToken cancellationToken, IProgress<double> progress);
        public abstract IEnumerable<TaskTriggerInfo> GetDefaultTriggers();
        public abstract string Name { get; }
        public abstract string Key { get; }
        public abstract string Description { get; }
        public abstract string Category { get; }
        
        
        /**
         * item是否存在相应
         */
        protected virtual bool HasAnyScraperProviderId(ReadOnlyCollection<AbstractScraper> scrapers, BaseItem item)
        {
            foreach (var scraper in scrapers)
            {
                if (item.HasProviderId(scraper.ProviderId))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual Dictionary<string, string> GetScraperFilter(ReadOnlyCollection<AbstractScraper> scrapers)
        {
            var filter = new Dictionary<string, string>();
            foreach (var scraper in scrapers)
            {
                filter.Add(scraper.ProviderId, string.Empty);
            }

            return filter;
        }
    }
}