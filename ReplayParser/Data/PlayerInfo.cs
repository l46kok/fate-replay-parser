using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Data
{
    public class PlayerInfo
    {
        public string PlayerName { get; set; }
        public int PlayerReplayId { get; set; } //Not zero-indexed. 1-12 are valid inputs.
        public int PlayerGameId { get; set; } //For some reason, replay writes different game Id than what's set in Fate map.
        public int RecordId { get; set; } //0x00 for game host, 0x16 for additional players (This info isn't really relevant)
        public int Team { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public List<string> ItemPurchaseList { get; } // Stores purchased item by their itemId (E.G: I000,I001...)
        public string ServantId { get; set; }
        public bool IsObserver { get; set; }
        public PlayerInfo(string playerName, int playerId, int recordId)
        {
            PlayerName = playerName;
            PlayerReplayId = playerId;
            RecordId = recordId;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            ItemPurchaseList = new List<string>();
            IsObserver = false;
        }
    }
}
