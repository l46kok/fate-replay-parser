using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FateReplayParser.Utility
{
    public class ConfigHandler
    {
        public string ReplayPath { get; private set; }
        public string ParsedReplayPath { get; private set; }
        public string ErrorReplayPath { get; private set; }
        public string ServerName { get; private set; }
        public string MapVersion { get; private set; }
        public string InvalidReplayPath { get; private set; }
        public int ParseTimePeriod { get; private set; }
        public string DatabaseServer { get; private set; }
        public uint DatabasePort { get; private set; }
        public string DatabaseUserName { get; private set; }
        public string DatabasePassword { get; private set; }
        public string DatabaseName { get; private set; }

        private string _configFilePath;
        private readonly List<string> _fileContent = new List<string>();
        public ConfigHandler(string configFilePath)
        {
            _configFilePath = configFilePath;
            if (File.Exists(configFilePath))
            {
                _fileContent = new List<string>(File.ReadAllLines(configFilePath));
            }
        }

        public bool IsConfigFileValid()
        {
            return _fileContent.Any();
        }

        public void LoadConfig()
        {
            ReplayPath = GetConfigString("replaypath");
            ParsedReplayPath = GetConfigString("parsedreplaypath");
            ErrorReplayPath = GetConfigString("errorreplaypath");
            InvalidReplayPath = GetConfigString("invalidreplaypath");
            ServerName = GetConfigString("server");
            MapVersion = GetConfigString("mapversion");
            int parsedInt;
            if (int.TryParse(GetConfigString("parsetimeperiod"), out parsedInt))
            {
                ParseTimePeriod = parsedInt;
            }
            DatabaseServer = GetConfigString("databaseserver");
            if (int.TryParse(GetConfigString("databaseport"), out parsedInt))
            {
                DatabasePort = (uint) parsedInt;
            }
            DatabaseUserName = GetConfigString("databaseusername");
            DatabasePassword = GetConfigString("databasepassword");
            DatabaseName = GetConfigString("databasename");
        }

        private string GetConfigString(string key)
        {
            foreach (string line in _fileContent)
            {
                if (String.IsNullOrEmpty(line))
                    continue;
                if (line[0] == '#') //Config Comment
                    continue;
                string[] configValueArray = line.Split('=');
                if (configValueArray.Length != 2)
                    continue;
                if (configValueArray[0] == key)
                    return configValueArray[1];
            }   
            return String.Empty;
        }
    }
}
