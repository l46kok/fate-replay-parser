using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FateReplayParser.Data;
using NLog;

namespace FateReplayParser.Validators
{
    //Determine if the game meets certain criterias to be inserted into DB
    public static class FateGameValidator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        public static bool IsFateGameValid(ReplayData fateReplayData)
        {
            if (!IsGameDurationValid(fateReplayData))
                return false;
            if (fateReplayData.GameMode != GameMode.DM)
            {
                logger.Trace("Skipping stats insert: Non DeathMatch game.");
                return false;
            }

            if (fateReplayData.IsPracticeMode)
            {
                logger.Trace("Skipping stats insert: Practice mode.");
                return false;
            }

            if (fateReplayData.PlayerCount < 6)
            {
                logger.Trace($"Skipping stats insert: Not Enough players: (Players: {fateReplayData.PlayerCount})");
                return false;
            }

            int maxRoundScore = Math.Max(fateReplayData.TeamOneVictoryCount, fateReplayData.TeamTwoVictoryCount);
            if (maxRoundScore < 10)
            {
                logger.Trace($"Skipping stats insert: Max round score is less than 10 (MaxRoundScore: {maxRoundScore})");
                return false;
            }

            if (fateReplayData.PlayerInfoList.Count(x => x.Team == 0) !=
                fateReplayData.PlayerInfoList.Count(x => x.Team == 1))
            {
                logger.Trace($"Skipping stats insert: Uneven Teams");
                return false;
            }
            return true;
        }
        //Length must be 20 minutes or more
        private static bool IsGameDurationValid(ReplayData fateReplayData)
        {
            TimeSpan gameDuration = new TimeSpan(0, 0, 0, 0, (int)fateReplayData.ReplayHeader.ReplayLength);
            if (gameDuration.TotalMinutes < 25)
            {
                logger.Trace("Game duration is too short. Must be 25 minutes or more");
                return false;
            }
            return true;
        }
    }
}
