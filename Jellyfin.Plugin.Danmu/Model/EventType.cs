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
    Force,

    /// <summary>
    /// 单集强刷事件：仅刷新本集弹幕（本集已有来源→直接强刷；否则需所属季已匹配才能按集号补下）.
    /// </summary>
    ForceSingle
}
