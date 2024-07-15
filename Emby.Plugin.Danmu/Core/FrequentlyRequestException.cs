using System;

namespace Emby.Plugin.Danmu.Core
{
    public class FrequentlyRequestException : Exception
    {
        public FrequentlyRequestException(Exception ex) : base("Request tool frequently", ex)
        {

        }
    }
}