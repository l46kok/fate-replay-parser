using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace ReplayParser.Log
{
    public static class Log
    {
        public static Logger Instance { get; private set; }
        static Log()
        {
            Instance = LogManager.GetCurrentClassLogger();
        }
    }
}
