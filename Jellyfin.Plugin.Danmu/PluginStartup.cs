using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Net;
using Jellyfin.Plugin.Danmu.Model;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Providers;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu
{
    public class PluginStartup : IServerEntryPoint, IDisposable
    {
        private readonly ILibraryManager _libraryManager;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;
        private readonly ILogger<PluginStartup> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginStartup"/> class.
        /// </summary>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
        public PluginStartup(
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            LibraryManagerEventsHelper libraryManagerEventsHelper,
            IFileSystem fileSystem,
            IServerApplicationHost appHost)
        {
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger<PluginStartup>();
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
        }

        public Task RunAsync()
        {
            _libraryManager.ItemAdded += LibraryManagerItemAdded;
            _libraryManager.ItemUpdated += LibraryManagerItemUpdated;
            // _libraryManager.ItemRemoved += LibraryManagerItemRemoved;

            return Task.CompletedTask;
        }




        /// <summary>
        /// Library item was added.
        /// </summary>
        /// <param name="sender">The sending entity.</param>
        /// <param name="itemChangeEventArgs">The <see cref="ItemChangeEventArgs"/>.</param>
        private void LibraryManagerItemAdded(object sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            // Don't do anything if it's not a supported media type
            if (itemChangeEventArgs.Item is not Movie and not Episode and not Series and not Season)
            {
                return;
            }

            // 当剧集没有SXX/Season XX季文件夹时，LocationType就是Virtual，动画经常没有季文件夹
            if (itemChangeEventArgs.Item.LocationType == LocationType.Virtual && itemChangeEventArgs.Item is not Season)
            {
                return;
            }

            _libraryManagerEventsHelper.QueueItem(itemChangeEventArgs.Item, EventType.Add);
        }


        /// <summary>
        /// Library item was updated.
        /// </summary>
        /// <param name="sender">The sending entity.</param>
        /// <param name="itemChangeEventArgs">The <see cref="ItemChangeEventArgs"/>.</param>
        private void LibraryManagerItemUpdated(object sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            // Don't do anything if it's not a supported media type
            if (itemChangeEventArgs.Item is not Movie and not Episode and not Series and not Season)
            {
                return;
            }

            if (itemChangeEventArgs.Item.LocationType == LocationType.Virtual && itemChangeEventArgs.Item is not Season)
            {
                return;
            }

            _libraryManagerEventsHelper.QueueItem(itemChangeEventArgs.Item, EventType.Update);
        }

        /// <summary>
        /// Library item was removed.
        /// </summary>
        /// <param name="sender">The sending entity.</param>
        /// <param name="itemChangeEventArgs">The <see cref="ItemChangeEventArgs"/>.</param>
        private void LibraryManagerItemRemoved(object sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            if (itemChangeEventArgs.Item is not Movie and not Episode and not Series and not Season)
            {
                return;
            }

            if (itemChangeEventArgs.Item.LocationType == LocationType.Virtual)
            {
                return;
            }

            _libraryManagerEventsHelper.QueueItem(itemChangeEventArgs.Item, EventType.Remove);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Removes event subscriptions on dispose.
        /// </summary>
        /// <param name="disposing"><see cref="bool"/> indicating if object is currently disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _libraryManager.ItemAdded -= LibraryManagerItemAdded;
                _libraryManager.ItemUpdated -= LibraryManagerItemUpdated;
                // _libraryManager.ItemRemoved -= LibraryManagerItemRemoved;
                _libraryManagerEventsHelper.Dispose();
            }
        }

    }
}
