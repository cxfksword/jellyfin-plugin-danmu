using System;

namespace Emby.Plugin.Danmu.Core
{
    public class CanIgnoreException : Exception
    {
        public CanIgnoreException(string message) : base(message)
        {
        }

        /// <summary>
        /// Don't display call stack as it's irrelevant
        /// </summary>
        public override string StackTrace
        {
            get { return ""; }
        }
    }
}