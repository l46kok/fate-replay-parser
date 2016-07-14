using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Copyright 2013 11 01 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Data class for FRS Event
 * 
 * The event is injected into the replay file based on JASS native call of SyncStoredInteger.
 * 
 * The following describes the format of the events. Here's an example of what the function call would look like based on the protocol: 

    call SyncStoredInteger(gc, "GameMode","DM");

    Items inside brackets {} denotes possible inputs while double slash // denotes delimiter. Keys aren't case sensitive.
    
    Mission Key---Key---Notes

    "GameMode"---"{DM}", "{CTF}", "{Ranked}"---Ranked denotes ranked games. See http://cafe.naver.com/ufw/15221
    "RoundVictory"---"{T1}"---"{T2}"---"{Draw}"	
    "ServantSelection"---"PlayerReplayId//HeroId"---Player indices from 1-12, HeroId from world editor object Id such as 'H000'
    "Kill"---"PlayerId1//PlayerId2"---In the order of PlayerId1 killing PlayerId2. Note that each call increments kill/death of respective players by 1
    "Assist"---"PlayerReplayId"	
    "Attribute"---"Attribute Object Id"	
    "Stat"---"PlayerReplayId//Stat Object Id"---Each call increments stat learned by 1
    "Forfeit"---"{T1}"---"{T2}"
 */

namespace ReplayParser.Data
{
    public class FRSEvent
    {
        public string EventId { get; set; }
        public string GameCacheName { get; set; }
        public string EventCategory { get; set; }
        public string EventDetail { get; set; }

        public FRSEvent(string eventId, string gameCacheName, string eventCategory, string eventDetail)
        {
            EventId = eventId;
            GameCacheName = gameCacheName;
            EventCategory = eventCategory;
            EventDetail = eventDetail;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FRSEvent)) return obj.Equals(this); // defer to other object
            FRSEvent other = (FRSEvent)obj;
            return EventId == other.EventId; // check field equality
        }
        public override int GetHashCode()
        {
            int hc = 13;
            hc += EventId.GetHashCode() * 27;

            return hc;
        }
    }
}
