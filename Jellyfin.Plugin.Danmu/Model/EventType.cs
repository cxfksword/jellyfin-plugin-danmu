using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Model;

/// <summary>
/// Enum EventType.
/// </summary>
public enum EventType
{
    /// <summary>
    /// The addevent.
    /// </summary>
    Add,

    /// <summary>
    /// The remove event.
    /// </summary>
    Remove,

    /// <summary>
    /// The update event.
    /// </summary>
    Update,

    /// <summary>
    /// The force update event.
    /// </summary>
    Force
}
