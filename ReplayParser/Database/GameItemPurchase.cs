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
    
    public partial class GameItemPurchase
    {
        public int GameItemPurchaseID { get; set; }
        public int FK_ItemID { get; set; }
        public int FK_GamePlayerDetailID { get; set; }
        public int ItemPurchaseCount { get; set; }
    
        public virtual ItemInfo iteminfo { get; set; }
    }
}
