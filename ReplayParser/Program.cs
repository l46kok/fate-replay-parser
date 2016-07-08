/*
 * Copyright 2013 09 01 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Main entry point of Replay Parser
 */

using System;
using System.IO;
using NDesk.Options;
using NLog;
using ReplayParser.Data;
using ReplayParser.Database;

namespace ReplayParser
{
    internal class Program
    {
        private const string DEFAULT_CONFIG_FILE_PATH = "config.cfg";
        private static string _parsedReplayDirectory = "ParsedReplay";
        private static string _errorReplayDirectory = "ErrorReplay";
        private static string _replayFileDirectory = String.Empty;
        private static string _serverName = String.Empty;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            logger.Trace("Started Replay Parsing module");
            //Parse command line arguments
            //
            OptionSet options = new OptionSet()
                .Add("p=|input=", p => _replayFileDirectory = p)
                .Add("?|h|help", h => DisplayHelp());
            options.Parse(args);

            ConfigHandler configHandler = new ConfigHandler(DEFAULT_CONFIG_FILE_PATH);
            if (!configHandler.IsConfigFileValid())
                logger.Trace("Error loading default config file: {0}", DEFAULT_CONFIG_FILE_PATH);
            else
            {
                logger.Trace("Loading default config file: {0}", DEFAULT_CONFIG_FILE_PATH);
                configHandler.LoadConfig();
            }

            if (!String.IsNullOrEmpty(_replayFileDirectory))
            {
                logger.Trace("Path provided for Warcraft 3 Replay File Directory: {0}", _replayFileDirectory);
            }
            else if (configHandler.IsConfigFileValid())
            {
                _replayFileDirectory = configHandler.ReplayPath;
                logger.Trace("Using Warcraft 3 Replay File Directory from config file: {0}", _replayFileDirectory);
            }
            else
            {
                logger.Trace("Replay directory not set. Terminating.");
                return;
            }

            if (!String.IsNullOrEmpty(configHandler.ServerName))
                _serverName = configHandler.ServerName;
            else
            {
                logger.Trace("Server name not set. Terminating.");
                return;
            }

            if (!String.IsNullOrEmpty(configHandler.ParsedReplayPath))
                _parsedReplayDirectory = configHandler.ParsedReplayPath;

            if (!String.IsNullOrEmpty(configHandler.ErrorReplayPath))
                _errorReplayDirectory = configHandler.ErrorReplayPath;

            if (!Directory.Exists(_replayFileDirectory))
            {
                logger.Trace("Invalid path set for Warcraft 3 replay files. Check your path.");
                return;
            }

            DirectoryInfo replayDirectory = new DirectoryInfo(_replayFileDirectory);
            DirectoryInfo parsedReplayDirectory = new DirectoryInfo(_parsedReplayDirectory);
            DirectoryInfo errorReplayDirectory = new DirectoryInfo(_errorReplayDirectory);
            if (!parsedReplayDirectory.Exists)
                parsedReplayDirectory = Directory.CreateDirectory(_parsedReplayDirectory);
            if (!errorReplayDirectory.Exists)
                errorReplayDirectory = Directory.CreateDirectory(_errorReplayDirectory);
            
            foreach (var file in replayDirectory.GetFiles())
            {
                try
                {
                    logger.Trace("-----------------------------------------");
                    logger.Trace("Started parsing replay file: " + file.Name);
                    Parser.FateReplayParser fateReplayParser = new Parser.FateReplayParser(file.FullName);
                    ReplayData fateReplayData = fateReplayParser.ParseReplayData();
                    fateReplayData.MapVersion = configHandler.MapVersion;
                    logger.Trace("Finished parsing replay file: " + file.Name);
                    
                    
                    FateDBModule dbModule = new FateDBModule();

                    logger.Trace("Inserting replay data into database");
                    dbModule.InsertReplayData(fateReplayData, _serverName);
                    
                    /*                    if (FateGameValidator.IsFateGameValid(fateReplayData))
                                        {
                                            logger.Trace("Inserting replay data into database");
                                            dbModule.InsertReplayData(fateReplayData, _serverName);
                                        }*/

                    file.MoveTo(Path.Combine(parsedReplayDirectory.FullName,file.Name));

                }
                catch (Exception ex)
                {
                    logger.Error("Error occurred on parsing the following replay file: " + file.Name + Environment.NewLine);
                    logger.Trace(ex + Environment.NewLine);
                    string pathToMoveTo = Path.Combine(errorReplayDirectory.FullName, file.Name);
                    if (File.Exists(pathToMoveTo))
                    {
                        file.Delete();
                    }
                    else
                    {
                        file.MoveTo(Path.Combine(errorReplayDirectory.FullName, file.Name));
                    }
                }
            }
            logger.Trace("Replay parsing complete");
        }

        static void DisplayHelp()
        {
            
            return;
        }
    }
}
