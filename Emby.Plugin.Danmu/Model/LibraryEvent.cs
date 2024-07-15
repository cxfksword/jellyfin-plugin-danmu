using MediaBrowser.Controller.Entities;

namespace Emby.Plugin.Danmu.Model
{
    public class LibraryEvent
    {
        public BaseItem Item { get; set; }

        public EventType EventType { get; set; }
    }
}