/*
 * Copyright 2013 09 01 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Data class for replay. 
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using FateReplayParser.Utility;

namespace FateReplayParser.Data
{
    public enum GameMode
    {
        DM,
        CTF,
        Ranked
    };

    public enum GameResult
    {
        T1W,
        T2W,
        NONE
    };

    public enum GamePlayerResult
    {
        WIN,
        LOSS,
        NONE
    }
    public class ReplayData
    {
        private readonly Dictionary<int, PlayerInfo> _playerInfoDicByGameId = new Dictionary<int, PlayerInfo>();
        private readonly Dictionary<int, PlayerInfo> _playerInfoDicByReplayId = new Dictionary<int, PlayerInfo>();
        private readonly Dictionary<string, PlayerInfo> _playerInfoDicByName = new Dictionary<string, PlayerInfo>();
        private byte[] _replayFileBytes;
        private readonly List<string> _gameChatMessage = new List<string>();

        public List<string> GameChatMessage => _gameChatMessage;

        public GameMode GameMode { get; set; }
        public bool IsPracticeMode { get; set; }
        public ReplayHeader ReplayHeader { get; set; }
        public List<DataBlock> DataBlockList { get; private set; }
        public List<PlayerInfo> PlayerInfoList => _playerInfoDicByName.Values.ToList();
        public string GameName { get; set; }
        public uint PlayerCount { get; set; }
        public int TeamOneVictoryCount { get; set; }
        public int TeamTwoVictoryCount { get; set; }
        public int DrawCount { get; set; }
        public DateTime GameDateTime { get; set; }
        public string MapVersion { get; set; }
        public string ReplayUrl { get; set; }

        public ReplayData(byte[] replayFileBytes)
        {
            _replayFileBytes = replayFileBytes;
            DataBlockList = new List<DataBlock>();
            PlayerCount = 0;
            TeamOneVictoryCount = 0;
            TeamTwoVictoryCount = 0;
            DrawCount = 0;
            IsPracticeMode = false;
        }

        public void AddPlayerInfo(PlayerInfo player)
        {
            _playerInfoDicByName.Add(player.PlayerName, player);
            _playerInfoDicByReplayId.Add(player.PlayerReplayId,player);
        }

        public void AddPlayerInfoByGameId(int gameId, PlayerInfo player)
        {
            _playerInfoDicByGameId.Add(player.PlayerGameId, player);
        }

        public void AddDataBlock(DataBlock dataBlock)
        {
            DataBlockList.Add(dataBlock);
        }

        public void AddChatMessage(string chatMessage)
        {
            _gameChatMessage.Add(chatMessage);
        }

        public PlayerInfo GetPlayerInfoByPlayerReplayId(int playerReplayId)
        {
            return _playerInfoDicByReplayId[playerReplayId];
        }

        public PlayerInfo GetPlayerInfoByPlayerGameId(int playerGameId)
        {
            return _playerInfoDicByGameId[playerGameId];
        }

        public PlayerInfo GetPlayerInfoByPlayerName(string playerName)
        {
            return _playerInfoDicByName[playerName];
        }
    }
}
