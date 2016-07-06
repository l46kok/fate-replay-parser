//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReplayParser
{
    using System;
    using System.Collections.Generic;
    
    public partial class Server
    {
        public Server()
        {
            this.GamePlayerDetail = new HashSet<GamePlayerDetail>();
            this.Player = new HashSet<Player>();
            this.PlayerBanInfo = new HashSet<PlayerBanInfo>();
            this.PlayerHeroStat = new HashSet<PlayerHeroStat>();
            this.PlayerStat = new HashSet<PlayerStat>();
            this.Ranking = new HashSet<Ranking>();
            this.Game = new HashSet<Game>();
        }
    
        public int ServerID { get; set; }
        public string ServerName { get; set; }
        public bool IsServiced { get; set; }
    
        public virtual ICollection<GamePlayerDetail> GamePlayerDetail { get; set; }
        public virtual ICollection<Player> Player { get; set; }
        public virtual ICollection<PlayerBanInfo> PlayerBanInfo { get; set; }
        public virtual ICollection<PlayerHeroStat> PlayerHeroStat { get; set; }
        public virtual ICollection<PlayerStat> PlayerStat { get; set; }
        public virtual ICollection<Ranking> Ranking { get; set; }
        public virtual ICollection<Game> Game { get; set; }
    }
}
