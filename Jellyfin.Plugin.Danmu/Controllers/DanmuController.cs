using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Providers;
using Jellyfin.Plugin.Danmu.Providers;
using System.Runtime.InteropServices;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.Danmu.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("/plugin/danmu")]
    public class DanmuController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DanmuController"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public DanmuController(
            IFileSystem fileSystem,
            LibraryManagerEventsHelper libraryManagerEventsHelper,
            ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
        }

        /// <summary>
        /// 获取弹幕文件内容.
        /// </summary>
        /// <returns>xml弹幕文件内容</returns>
        [Route("{id}")]
        [HttpGet]
        public async Task<DanmuFileInfo> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new DanmuFileInfo();
            }

            var currentItem = _libraryManager.GetItemById(id);
            if (currentItem == null)
            {
                return new DanmuFileInfo();
            }

            var danmuPath = Path.Combine(currentItem.ContainingFolderPath, currentItem.FileNameWithoutExtension + ".xml");
            var fileMeta = _fileSystem.GetFileInfo(danmuPath);
            if (!fileMeta.Exists)
            {
                return new DanmuFileInfo();
            }

            var domain = Request.Scheme + System.Uri.SchemeDelimiter + Request.Host;
            return new DanmuFileInfo() { Url = string.Format("{0}{1}{2}", domain, "/plugin/danmu/raw/", id) };
        }

        /// <summary>
        /// 获取弹幕文件内容.
        /// </summary>
        /// <returns>xml弹幕文件内容</returns>
        [Route("raw/{id}")]
        [HttpGet]
        public async Task<ActionResult> GetFile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ResourceNotFoundException();
            }

            var currentItem = _libraryManager.GetItemById(id);
            if (currentItem == null)
            {
                throw new ResourceNotFoundException();
            }

            var danmuPath = Path.Combine(currentItem.ContainingFolderPath, currentItem.FileNameWithoutExtension + ".xml");
            var fileMeta = _fileSystem.GetFileInfo(danmuPath);
            if (!fileMeta.Exists)
            {
                throw new ResourceNotFoundException();
            }

            return File(System.IO.File.ReadAllBytes(danmuPath), "text/xml");
        }


        /// <summary>
        /// 重新获取对应的弹幕id.
        /// </summary>
        /// <returns>请求结果</returns>
        [Route("")]
        [HttpGet]
        public async Task<String> Refresh(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ResourceNotFoundException();
            }

            var item = _libraryManager.GetItemById(id);
            if (item == null)
            {
                throw new ResourceNotFoundException();
            }

            if (item is Movie || item is Series || item is Season || item is Episode)
            {
                _libraryManagerEventsHelper.QueueItem(item, Model.EventType.Add);
            }

            return "ok";
        }
    }
}
