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
    
    public partial class commandsealuse
    {
        public int CommandSealUseID { get; set; }
        public string CommandSealAbilID { get; set; }
        public int UseCount { get; set; }
        public int FK_GamePlayerDetailID { get; set; }
    }
}
