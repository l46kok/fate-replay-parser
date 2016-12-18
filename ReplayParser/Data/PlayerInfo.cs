using System.Collections.Generic;

namespace FateReplayParser.Data
{
    public class PlayerInfo
    {
        public string PlayerName { get; private set; }
        public int PlayerReplayId { get; private set; } //Not zero-indexed. 1-12 are valid inputs.
        public int PlayerGameId { get; set; } //For some reason, replay writes different game Id than what's set in Fate map.
        public int RecordId { get; set; } //0x00 for game host, 0x16 for additional players (This info isn't really relevant)
        public int Team { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int ServantLevel { get; set; }
        public double DamageDealt { get; set; }
        public double DamageTaken { get; set; }
        public List<string> ItemPurchaseList { get; } // Stores purchased item by their itemId (E.G: I000,I001...)
        public List<string> GodsHelpList { get; }
        public List<string> AttributeList { get; }
        public List<string> StatList { get; }
        public List<string> CommandSealList { get; }
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
            DamageDealt = 0;
            DamageTaken = 0;
            ServantLevel = 1;
            ItemPurchaseList = new List<string>();
            GodsHelpList = new List<string>();
            AttributeList = new List<string>();
            StatList = new List<string>();
            CommandSealList = new List<string>();
            IsObserver = false;
        }
    }
}
