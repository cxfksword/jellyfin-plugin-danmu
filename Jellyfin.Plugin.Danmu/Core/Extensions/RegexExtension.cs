using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core.Extensions
{
    public static class RegexExtension
    {
        public static string FirstMatch(this Regex reg, string text, string defaultVal = "")
        {
            var match = reg.Match(text);
            if (match.Success)
            {
                return match.Value;
            }

            return defaultVal;
        }

        public static string FirstMatchGroup(this Regex reg, string text, string defaultVal = "")
        {
            var match = reg.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            return defaultVal;
        }
    }
}