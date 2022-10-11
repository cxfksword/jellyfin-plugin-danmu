using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Danmu.Model;

public class LibraryEvent
{
    public BaseItem Item { get; set; }

    public EventType EventType { get; set; }
}
