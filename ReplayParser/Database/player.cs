//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FateReplayParser.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class player
    {
        public int PlayerID { get; set; }
        public int FK_ServerID { get; set; }
        public string PlayerName { get; set; }
        public System.DateTime RegDate { get; set; }
        public bool IsBanned { get; set; }
        public System.DateTime LastUpdatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public Nullable<System.DateTime> BannedOn { get; set; }
        public Nullable<System.DateTime> UnbannedOn { get; set; }
    
        public virtual server server { get; set; }
    }
}
