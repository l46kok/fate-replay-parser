/*
 * Copyright 2013 09 01 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Main entry point of Replay Parser
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using FateReplayParser.Data;
using FateReplayParser.Database;
using FateReplayParser.Utility;
using FateReplayParser.Validators;
using NDesk.Options;
using NLog;

namespace FateReplayParser
{
    internal class Program
    {
        private const int CONSOLE_CLEAR_DISPLAY_LIMIT = 50; //Clears console every nth time parsing has run
        private const string DEFAULT_CONFIG_FILE_PATH = "config.cfg";
        private static string _parsedReplayDirectory = "ParsedReplay";
        private static string _errorReplayDirectory = "ErrorReplay";
        private static string _replayFileDirectory = String.Empty;
        private static string _serverName = String.Empty;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static int _parsePeriod = 30; // Seconds;
        private static Timer _parseTimer;
        private static readonly Stopwatch _stopWatch = new Stopwatch();
        private static int _consoleClearCounter = 0;
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

            if (configHandler.ParseTimePeriod < 5)
            {
                logger.Trace("Setting minimum default parsing period of 5 seconds.");
                _parsePeriod = 5;
            }
            else
            {
                logger.Trace($"Setting parsing period of {configHandler.ParseTimePeriod} seconds from config file.");
                _parsePeriod = configHandler.ParseTimePeriod;
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
            _parseTimer = new Timer(_parsePeriod*1000);
            _parseTimer.Elapsed += (sender, e) => RunParser(sender, e, configHandler);
            RunParser(null, null, configHandler);

            while (true)
            {
                Console.ReadLine();
            }
        }

        private static void RunParser(object sender, ElapsedEventArgs e, ConfigHandler configHandler)
        {
            _parseTimer.Stop();
            _stopWatch.Reset();
            _stopWatch.Start();
            DirectoryInfo replayDirectory = new DirectoryInfo(_replayFileDirectory);
            DirectoryInfo parsedReplayDirectory = new DirectoryInfo(_parsedReplayDirectory);
            DirectoryInfo errorReplayDirectory = new DirectoryInfo(_errorReplayDirectory);
            if (!parsedReplayDirectory.Exists)
                parsedReplayDirectory = Directory.CreateDirectory(_parsedReplayDirectory);
            if (!errorReplayDirectory.Exists)
                errorReplayDirectory = Directory.CreateDirectory(_errorReplayDirectory);
            if (!replayDirectory.GetFiles().Any())
            {
                logger.Trace($"No replay files found in directory.");
            }
            else
            {
                foreach (var file in replayDirectory.GetFiles())
                {
                    try
                    {
                        logger.Trace("-----------------------------------------");
                        logger.Trace("Started parsing replay file: " + file.Name);
                        FateReplayParser.Parser.FateReplayParser fateReplayParser = new FateReplayParser.Parser.FateReplayParser(file.FullName);
                        ReplayData fateReplayData = fateReplayParser.ParseReplayData();
                        fateReplayData.MapVersion = configHandler.MapVersion;
                        logger.Trace("Finished parsing replay file: " + file.Name);

                        if (FateGameValidator.IsFateGameValid(fateReplayData))
                        {
                            logger.Trace("Inserting replay data into database");
                            FateDBModule dbModule = new FateDBModule();
                            dbModule.InsertReplayData(fateReplayData, _serverName);
                        }
                        else
                        {
                            logger.Trace("Game is not valid. Database insert skipped.");
                        }

                        string pathToMoveTo = Path.Combine(parsedReplayDirectory.FullName, file.Name);
                        if (File.Exists(pathToMoveTo))
                        {
                            logger.Trace($"Duplicate file name found for {file.Name} at ParsedReplay directory. File deleted.");
                            file.Delete();
                        }
                        else
                        {
                            file.MoveTo(Path.Combine(parsedReplayDirectory.FullName, file.Name));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error occurred on parsing the following replay file: " + file.Name + Environment.NewLine);
                        logger.Trace(ex + Environment.NewLine);
                        string pathToMoveTo = Path.Combine(errorReplayDirectory.FullName, file.Name);
                        if (File.Exists(pathToMoveTo))
                        {
                            logger.Trace($"Duplicate file name found for {file.Name} at ErrorReplay directory. File deleted.");
                            file.Delete();
                        }
                        else
                        {
                            file.MoveTo(Path.Combine(errorReplayDirectory.FullName, file.Name));
                        }
                    }
                }
                logger.Trace($"Replay parsing complete [Elapsed: {_stopWatch.Elapsed.TotalSeconds} seconds]");
            }

            _consoleClearCounter++;
            if (_consoleClearCounter >= CONSOLE_CLEAR_DISPLAY_LIMIT)
            {
                _consoleClearCounter = 0;
                Console.Clear();
            }
            _stopWatch.Stop();
            _parseTimer.Start();
        }

        static void DisplayHelp()
        {
            
            return;
        }
    }
}
