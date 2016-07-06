using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Data;

namespace ReplayParser.Utility
{
    public class FRSEventComparer : IEqualityComparer<FRSEvent>
    {
        public bool Equals(FRSEvent x, FRSEvent y)
        {
            return x.GameCacheName == y.GameCacheName && x.EventCategory == y.EventCategory && x.EventDetail == y.EventDetail; 
        }
        public int GetHashCode(FRSEvent obj)
        {
            int hc = 13;
            hc += obj.GameCacheName.GetHashCode() * 27;
            hc += obj.EventCategory.GetHashCode() * 27;
            hc += obj.EventDetail.GetHashCode() * 27;

            return hc;
        }
    }
}
