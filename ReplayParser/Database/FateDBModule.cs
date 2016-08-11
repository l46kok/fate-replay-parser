using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using FateReplayParser.Data;
using FateReplayParser.Utility;
using NLog;

namespace FateReplayParser.Database
{
    public class FateDBModule
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public void InsertReplayData(ReplayData replayData, string serverName)
        {
            using (var db = new frsDb())
            {
                using (var trans = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (!db.server.Any(x => x.ServerName == serverName && x.IsServiced))
                            throw new Exception(
                                String.Format("DB Error: Either server name doesn't exist or it is not serviced: {0}",
                                              serverName));

                        server dbServer = db.server.FirstOrDefault(x => x.ServerName == serverName);
                        //Player collection based on server
                        var dbPlayers = db.Set<player>().Where(x => x.FK_ServerID == dbServer.ServerID);

                        List<player> fatePlayerList = AddPlayerList(replayData, dbPlayers, dbServer, db);

                        db.SaveChanges();

                        game fateGame = GetNewGame(replayData, dbServer, db);
                        db.game.Add(fateGame);

                        List<gameplayerdetail> fateGamePlayerDetailList = GetGamePlayerDetailList(replayData,
                                                                                                  fatePlayerList,
                                                                                                  fateGame, dbServer, db);
                        db.gameplayerdetail.AddRange(fateGamePlayerDetailList);

                        AddPlayerStatToDatabase(replayData, fatePlayerList, db, dbServer, fateGame);
                        AddPlayerHeroStatToDatabase(replayData, fatePlayerList, db, dbServer);
                        db.SaveChanges(); //Save changes at this point to assign IDs to tables
                        AddItemPurchaseDetailToDatabase(replayData, db, fateGamePlayerDetailList, fatePlayerList);
                        db.SaveChanges(); 
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        logger.Error(ex.ToString());
                        throw;
                    }
                }
            }
        }

        private static void AddItemPurchaseDetailToDatabase(ReplayData replayData, frsDb db,
            List<gameplayerdetail> dbPlayerDetailList, List<player> dbPlayerList)
        {
            int maxGameItemPurchaseId = 0;
            if (db.gameitempurchase.Any())
            {
                maxGameItemPurchaseId = db.gameitempurchase.Max(x => x.GameItemPurchaseID) + 1;
            }
            foreach (player player in dbPlayerList)
            {
                PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerName(player.PlayerName);
                if (playerInfo == null)
                    throw new InvalidOperationException("Player Name could not be found from replay data (AIPDTD): " + player.PlayerName);
                var purchasedItemGroup = playerInfo.ItemPurchaseList.GroupBy(x => x)
                    .Select(group => new
                    {
                        ItemTypeID = group.Key,
                        PurchaseCount = group.Count()
                    });

                gameplayerdetail gpDetail = dbPlayerDetailList.First(x => x.FK_PlayerID == player.PlayerID);

                int spentGold = 0;
                foreach (var item in purchasedItemGroup)
                { 
                    iteminfo itemInfo = db.iteminfo.First(x => x.ItemTypeID == item.ItemTypeID);
                    gameitempurchase itemPurchaseRow = new gameitempurchase
                    {
                        GameItemPurchaseID = maxGameItemPurchaseId,
                        FK_GamePlayerDetailID = gpDetail.GamePlayerDetailID,
                        FK_ItemID = itemInfo.ItemID,
                        ItemPurchaseCount = item.PurchaseCount
                    };
                    spentGold += itemInfo.ItemCost*item.PurchaseCount;
                    db.gameitempurchase.Add(itemPurchaseRow);
                    maxGameItemPurchaseId++;
                }

                gpDetail.GoldSpent = spentGold;
            }
        }

        private static void AddPlayerHeroStatToDatabase(ReplayData replayData, IEnumerable<player> dbPlayers,
                                                        frsDb db, server dbServer)
        {
            foreach (player player in dbPlayers)
            {

                PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerName(player.PlayerName);
                if (playerInfo == null)
                    throw new Exception(String.Format("Player Name not found during PlayerHeroStat module. Input: {0}",
                                                      player.PlayerName));

                herotype playerHeroType = db.herotype.FirstOrDefault(x => x.HeroUnitTypeID == playerInfo.ServantId);

                playerherostat playerHeroStat =
                    db.playerherostat.FirstOrDefault(
                        x =>
                        x.FK_ServerID == dbServer.ServerID && x.FK_PlayerID == player.PlayerID &&
                        x.FK_HeroTypeID == playerHeroType.HeroTypeID);
                bool isNewHeroStat = false;
                if (playerHeroStat == null)
                {
                    playerHeroStat = new playerherostat();
                    playerHeroStat.FK_PlayerID = player.PlayerID;
                    playerHeroStat.FK_ServerID = dbServer.ServerID;
                    playerHeroStat.FK_HeroTypeID = playerHeroType.HeroTypeID;
                    db.playerherostat.Add(playerHeroStat);
                    isNewHeroStat = true;
                }
                playerHeroStat.HeroPlayCount++;
                playerHeroStat.TotalHeroKills += playerInfo.Kills;
                playerHeroStat.TotalHeroDeaths += playerInfo.Deaths;
                playerHeroStat.TotalHeroAssists += playerInfo.Assists;

                //Upsert
                db.playerherostat.Attach(playerHeroStat);
                var playerHeroStatEntry = db.Entry(playerHeroStat);
                playerHeroStatEntry.State = isNewHeroStat ? EntityState.Added : EntityState.Modified;
            }
        }

        private void AddPlayerStatToDatabase(ReplayData replayData, IEnumerable<player> dbPlayers, frsDb db,
                                             server dbServer, game fateGame)
        {
            foreach (player player in dbPlayers)
            {
                bool isNewPlayerStat = false;
                playerstat playerStat =
                    db.playerstat.FirstOrDefault(
                        x => x.FK_ServerID == dbServer.ServerID && x.FK_PlayerID == player.PlayerID);
                if (playerStat == null)
                {
                    playerStat = new playerstat();
                    playerStat.FK_PlayerID = player.PlayerID;
                    playerStat.FK_ServerID = dbServer.ServerID;
                    db.playerstat.Add(playerStat);
                    isNewPlayerStat = true;
                }
                playerStat.PlayCount++;

                PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerName(player.PlayerName);
                if (playerInfo == null)
                    throw new Exception(String.Format("Player Name not found during PlayerStatList module. Input: {0}",
                                                      player.PlayerName));

                if (fateGame.Result == GameResult.NONE.ToString())
                    continue;

                if (fateGame.Result == GameResult.T1W.ToString())
                {
                    if (playerInfo.Team == 0)
                        playerStat.Win++;
                    else if (playerInfo.Team == 1)
                        playerStat.Loss++;
                    else
                        throw new Exception(String.Format("Unexpected playerInfo team at PlayerStatListModule. Input: {0}",
                                                          playerInfo.Team));
                }
                else if (fateGame.Result == GameResult.T2W.ToString())
                {
                    if (playerInfo.Team == 0)
                        playerStat.Loss++;
                    else if (playerInfo.Team == 1)
                        playerStat.Win++;
                    else
                        throw new Exception(String.Format("Unexpected playerInfo team at PlayerStatListModule. Input: {0}",
                                                          playerInfo.Team));
                }
                else
                {
                    throw new Exception(String.Format("Unexpected GameResult enumeration at PlayerStatListModule. Input: {0}",
                                                      fateGame.Result));
                }

                //Upsert
                db.playerstat.Attach(playerStat);
                var playerStatEntry = db.Entry(playerStat);
                playerStatEntry.State = isNewPlayerStat ? EntityState.Added : EntityState.Modified;
                
            }
        }

        private List<gameplayerdetail> GetGamePlayerDetailList(ReplayData replayData, IEnumerable<player> dbPlayers, game fateGame, server dbServer,
                                                  frsDb db)
        {
            List<gameplayerdetail> fateGamePlayerDetailList = new List<gameplayerdetail>();
            foreach (player player in dbPlayers)
            {
                PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerName(player.PlayerName);
                if (playerInfo == null)
                    throw new Exception(String.Format("Player Name not found during GamePlayerDetailList module. Input: {0}",
                                                      player.PlayerName));

                gameplayerdetail fateGamePlayerDetail = new gameplayerdetail();
                fateGamePlayerDetail.FK_GameID = fateGame.GameID;
                fateGamePlayerDetail.FK_PlayerID = player.PlayerID;
                fateGamePlayerDetail.FK_ServerID = dbServer.ServerID;
                herotype playerHeroType = db.herotype.FirstOrDefault(x => x.HeroUnitTypeID == playerInfo.ServantId);
                if (playerHeroType == null)
                    throw new Exception(String.Format("DB Error: Unknown hero type id: {0}", playerInfo.ServantId));
                fateGamePlayerDetail.FK_HeroTypeID = playerHeroType.HeroTypeID;
                fateGamePlayerDetail.Kills = playerInfo.Kills;
                fateGamePlayerDetail.Deaths = playerInfo.Deaths;
                fateGamePlayerDetail.Assists = playerInfo.Assists;
                fateGamePlayerDetail.Team = (playerInfo.Team + 1).ToString();
                if (fateGame.Result == GameResult.NONE.ToString())
                    fateGamePlayerDetail.Result = GamePlayerResult.NONE.ToString();
                else if (fateGame.Result == GameResult.T1W.ToString())
                {
                    fateGamePlayerDetail.Result = playerInfo.Team == 0
                                                      ? GamePlayerResult.WIN.ToString()
                                                      : GamePlayerResult.LOSS.ToString();
                }
                else if (fateGame.Result == GameResult.T2W.ToString())
                {
                    fateGamePlayerDetail.Result = playerInfo.Team == 1
                                                      ? GamePlayerResult.WIN.ToString()
                                                      : GamePlayerResult.LOSS.ToString();
                }
                else
                {
                    throw new Exception(String.Format("Unexpected GameResult enumeration. Input: {0}",
                                                      fateGame.Result));
                }
                fateGamePlayerDetailList.Add(fateGamePlayerDetail);
            }
            return fateGamePlayerDetailList;
        }

        private game GetNewGame(ReplayData replayData, server dbServer, frsDb db)
        {
            int gameId = 0;
            if (db.game.Any())
                gameId = db.game.Max(g => g.GameID) + 1; //Generate Max GameID + 1

            game fateGame = new game
            {
                    GameID = gameId, 
                    GameName = replayData.GameName,
                    Log = String.Join("\n",replayData.GameChatMessage.ToArray()),
                    MatchType = replayData.GameMode.ToString(),
                    MapVersion = replayData.MapVersion,
                    Duration = new TimeSpan(0, 0, 0, 0, (int) replayData.ReplayHeader.ReplayLength),
                    PlayedDate = replayData.GameDateTime,
                    ReplayUrl = replayData.ReplayUrl,
                    FK_ServerID = dbServer.ServerID,
                    TeamOneWinCount = replayData.TeamOneVictoryCount,
                    TeamTwoWinCount = replayData.TeamTwoVictoryCount
                };
            if (replayData.TeamOneVictoryCount > replayData.TeamTwoVictoryCount)
                fateGame.Result = GameResult.T1W.ToString();
            else if (replayData.TeamOneVictoryCount < replayData.TeamTwoVictoryCount)
                fateGame.Result = GameResult.T2W.ToString();
            else
                fateGame.Result = GameResult.NONE.ToString();

            return fateGame;
        }

        private List<player> AddPlayerList(ReplayData replayData, IQueryable<player> dbPlayers, server dbServer,
                                             frsDb db)
        {
            List<player> playerList = new List<player>();
            int playerId = 0;
            
            foreach (PlayerInfo playerInfo in replayData.PlayerInfoList)
            {
                //Ignore observer
                if (playerInfo.IsObserver)
                    continue;

                player player = dbPlayers.FirstOrDefault(x => x.PlayerName == playerInfo.PlayerName);
                if (player != null)
                {
                    playerList.Add(player);
                    continue;
                }

                if (db.player.Any())
                    playerId = db.player.Max(g => g.PlayerID) + 1; //Generate Max GameID + 1

                player = new player
                {
                        FK_ServerID = dbServer.ServerID,
                        IsBanned = false,
                        LastUpdatedBy = "FateRankingSystem",
                        PlayerName = playerInfo.PlayerName,
                        RegDate = DateTime.Now,
                        LastUpdatedOn = DateTime.Now,
                        PlayerID = playerId
                    };
                playerId++;
                playerList.Add(player);
                db.player.Add(player);
            }
            return playerList;
        }
    }
}
