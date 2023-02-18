using System;

namespace Jellyfin.Plugin.Danmu.Core;

class CanIgnoreException : Exception
{
    public CanIgnoreException(string message) : base(message)
    {
    }

    /// <summary>
    /// Don't display call stack as it's irrelevant
    /// </summary>
    public override string StackTrace
    {
        get
        {
            return "";
        }
    }
}