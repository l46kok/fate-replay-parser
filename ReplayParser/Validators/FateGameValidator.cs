using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using ReplayParser.Data;

namespace ReplayParser.Validators
{
    //Determine if the game meets certain criterias to be inserted into DB
    public static class FateGameValidator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        public static bool IsFateGameValid(ReplayData fateReplayData)
        {
            //if (!IsGameDurationValid(fateReplayData))
            //    return false;

            if (fateReplayData.IsPracticeMode)
                return false;
            return true;
        }
        //1. Length must be 20 minutes or more
        private static bool IsGameDurationValid(ReplayData fateReplayData)
        {
            TimeSpan gameDuration = new TimeSpan(0, 0, 0, 0, (int)fateReplayData.ReplayHeader.ReplayLength);
            if (gameDuration.Minutes < 20)
            {
                logger.Trace("Game duration is too short. Must be 20 minutes or more");
                return false;
            }
            return true;
        }
    }
}
