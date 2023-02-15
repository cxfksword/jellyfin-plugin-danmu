using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Model;

public class SubtitleId
{
    public string ItemId { get; set; }

    public string Id { get; set; }

    public string ProviderId { get; set; }
}
