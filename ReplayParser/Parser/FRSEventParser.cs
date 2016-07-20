using System;
using System.Collections.Generic;
using System.IO;
using FateReplayParser.Data;
using FateReplayParser.Utility;

namespace FateReplayParser.Parser
{
    public static class FRSEventParser
    {
        private static readonly Dictionary<string, Action<FRSEvent, ReplayData>> _eventHandlerDic = new Dictionary
            <string, Action<FRSEvent, ReplayData>>
        {
            {"GameMode", ParseGameMode},
            {"PracticeMode", ParsePracticeMode},
            {"RoundVictory", ParseRoundVictory},
            {"ServantSelection", ParseServantSelection},
            {"Kill", ParseKill},
            {"Assist", ParseAssist},
            {"Suicide", ParseSuicide},
            {"Damage", ParseDamage},
            {"Attribute", ParseAttribute},
            {"LevelUp", ParseLevelUp},
            {"Stat", ParseStat},
            {"CommandSeal", ParseCommandSeal},
            {"GodsHelp", ParseGodsHelp},
            {"ItemBuy", ParseItemBuy},
            {"Forfeit", ParseForfeit},
        };


        /// <summary>
        /// Parses a list of FRSEvent, then fills replayData with parsed information
        /// </summary>
        /// <param name="frsEventCallList"></param>
        /// <param name="replayData"></param>
        public static void ParseEventAPI(IEnumerable<FRSEvent> frsEventCallList, ReplayData replayData)
        {
            foreach (FRSEvent frsEvent in frsEventCallList)
            {
                _eventHandlerDic[frsEvent.EventCategory](frsEvent, replayData);
            }
        }

        private static void ParseGameMode(FRSEvent frsEvent, ReplayData replayData)
        {
            string eventDetail = frsEvent.EventDetail;
            GameMode gameMode;
            if (!Enum.TryParse(eventDetail, true, out gameMode))
                throw new InvalidDataException($"Unexpected GameMode. Input {eventDetail}");
            replayData.GameMode = gameMode;
        }

        private static void ParsePracticeMode(FRSEvent frsEvent, ReplayData replayData)
        {
            replayData.IsPracticeMode = true;
        }

        private static void ParseRoundVictory(FRSEvent frsEvent, ReplayData replayData)
        {
            if (frsEvent.EventDetail.EqualsIgnoreCase("T1"))
                replayData.TeamOneVictoryCount++;
            else if (frsEvent.EventDetail.EqualsIgnoreCase("T2"))
                replayData.TeamTwoVictoryCount++;
            else if (frsEvent.EventDetail.EqualsIgnoreCase("Draw"))
                replayData.DrawCount++;
            else
                throw new InvalidDataException($"Unexpected RoundVictory data. Input {frsEvent.EventDetail}");
        }

        private static void ParseServantSelection(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] servantSelectionData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerName//PlayerReplayId//HeroId//TeamNumber
            if (servantSelectionData.Length != 4)
                throw new InvalidDataException($"Expected 4 inputs for ServantSelection event in method ParseServantSelection. Input {frsEvent.EventDetail}");
            string playerName = servantSelectionData[0];
            int playerGameId = int.Parse(servantSelectionData[1]);
            string servantId = servantSelectionData[2];
            int teamNumber = int.Parse(servantSelectionData[3]);
            if (teamNumber > 2 || teamNumber < 1)
                throw new InvalidDataException($"Incorrect TeamNumber found in ParseServantSelection. Input {teamNumber}");

            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerName(playerName);
            if (playerInfo == null)
                throw new InvalidDataException($"PlayerName could not be found in method ParseServantSelection. Input {playerName}");
            playerInfo.ServantId = servantId;
            playerInfo.PlayerGameId = playerGameId;

            //Mix teams switch teams around
            //We reassign team number here in case if it's been changed
            //Also not zero-index based, so we subtract one
            playerInfo.Team = teamNumber - 1;
        }

        private static void ParseKill(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] playerKillDeathData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //Player1(Killer)//Player2
            int killPlayerId = int.Parse(playerKillDeathData[0]);
            int deathPlayerId = int.Parse(playerKillDeathData[1]);
            PlayerInfo killerPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(killPlayerId);
            PlayerInfo victimPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(deathPlayerId);
            if (killerPlayerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (Killer) could not be found in method ParseKill. Input {killPlayerId}");
            if (victimPlayerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (Victim) could not be found in method ParseKill. Input {deathPlayerId}");
            if (killPlayerId != deathPlayerId)
            {
                killerPlayerInfo.Kills++;
            }
            victimPlayerInfo.Deaths++;
        }

        private static void ParseAssist(FRSEvent frsEvent, ReplayData replayData)
        {
            int assistPlayerId = int.Parse(frsEvent.EventDetail);
            PlayerInfo assistPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(assistPlayerId);
            if (assistPlayerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (Assist) could not be found in method ParseAssist. Input {assistPlayerId}");
            assistPlayerInfo.Assists++;
        }

        private static void ParseSuicide(FRSEvent frsEvent, ReplayData replayData)
        {
            int deathPlayerId = int.Parse(frsEvent.EventDetail);
            PlayerInfo victimPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(deathPlayerId);
            if (victimPlayerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (Victim) could not be found in method ParseSuicide (Suicide). Input {deathPlayerId}");
            victimPlayerInfo.Deaths++;
        }

        private static void ParseAttribute(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] attributeData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//StatSpellID
            int playerId = int.Parse(attributeData[0]);
            string attributeAbilId = attributeData[1];
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            if (playerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (playerInfo) could not be found in method ParseAttribute Input {playerId}");
            playerInfo.AttributeList.Add(attributeAbilId);
        }

        private static void ParseStat(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] statData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//StatSpellID
            int playerId = int.Parse(statData[0]);
            string statSpellId = statData[1];
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            if (playerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (playerInfo) could not be found in method ParseStat Input {playerId}");
            playerInfo.StatList.Add(statSpellId);
        }

        private static void ParseForfeit(FRSEvent frsEvent, ReplayData replayData)
        {

        }

        private static void ParseGodsHelp(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] godsHelpData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//GodsHelpID
            int playerId = int.Parse(godsHelpData[0]);
            string godsHelpSpellId = godsHelpData[1];
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            if (playerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (playerInfo) could not be found in method ParseGodsHelp Input {playerId}");
            playerInfo.GodsHelpList.Add(godsHelpSpellId);
        }

        private static void ParseCommandSeal(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] commandSealData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//SealSpellID
            int playerId = int.Parse(commandSealData[0]);
            string sealSpellId = commandSealData[1];
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            if (playerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (playerInfo) could not be found in method ParseCommandSeal Input {playerId}");
            playerInfo.CommandSealList.Add(sealSpellId);
        }

        private static void ParseItemBuy(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] itemBuyData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//ItemId
            int playerId = int.Parse(itemBuyData[0]);
            string itemId = itemBuyData[1];
            PlayerInfo buyingPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            if (buyingPlayerInfo == null)
                throw new InvalidDataException(
                    $"PlayerReplayId (buyingPlayerInfo) could not be found in method ParseItemBuy Input {playerId}");
            buyingPlayerInfo.ItemPurchaseList.Add(itemId);
        }

        private static void ParseDamage(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] damageData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//ItemId
            int sourcePlayerId = int.Parse(damageData[0]);
            int targetPlayerId = int.Parse(damageData[1]);
            double damageDealt = double.Parse(damageData[2]);
            PlayerInfo sourcePlayerInfo = replayData.GetPlayerInfoByPlayerGameId(sourcePlayerId);
            PlayerInfo targetPlayerInfo = replayData.GetPlayerInfoByPlayerGameId(targetPlayerId);
            sourcePlayerInfo.DamageDealt += damageDealt;
            targetPlayerInfo.DamageTaken += damageDealt;
        }


        private static void ParseLevelUp(FRSEvent frsEvent, ReplayData replayData)
        {
            string[] levelUpData = frsEvent.EventDetail.Split(new[] { "//" }, StringSplitOptions.None); //PlayerID//ItemId
            int playerId = int.Parse(levelUpData[0]);
            int newLevel = int.Parse(levelUpData[1]);
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerGameId(playerId);
            playerInfo.ServantLevel = newLevel;
        }
    }
}
