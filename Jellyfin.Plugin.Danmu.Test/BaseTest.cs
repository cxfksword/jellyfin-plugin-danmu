using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test
{

    [TestClass]
    public class BaseTest
    {
        protected ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
             builder.AddSimpleConsole(options =>
             {
                 options.IncludeScopes = true;
                 options.SingleLine = true;
                 options.TimestampFormat = "hh:mm:ss ";
             }));
    }
}