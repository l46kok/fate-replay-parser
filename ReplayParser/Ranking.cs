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
    
    public partial class Ranking
    {
        public int RankID { get; set; }
        public int FK_PlayerID { get; set; }
        public int FK_ServerID { get; set; }
        public int PlayerStatID { get; set; }
        public int PlayerRank { get; set; }
    
        public virtual Server Server { get; set; }
    }
}
