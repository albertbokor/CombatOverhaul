﻿using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.WorldObjects
{
    public class HostilityRaider : IExposable
    {
        public HostilityComp comp;

        private int ticksToRaid = -1;
        private GlobalTargetInfo targetInfo;        
        private float points = -1;

        public HostilityRaider()
        {
        }

        public void ThrottledTick()
        {
            if (ticksToRaid > 0 || points < 0)
            {
                ticksToRaid -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
            if (targetInfo.IsValid)
            {
                DoRaid();
            }
            points = -1;
            targetInfo = GlobalTargetInfo.Invalid;
            ticksToRaid = -1;
        }

        public bool TryRaid(Map targetMap, float points)
        {
            FactionStrengthTracker tracker =  comp.parent.Faction.GetStrengthTracker();
            if (tracker != null && !tracker.CanRaid)
            {
                return false;
            }
            if (points <= 0)
            {
                return false;
            }
            this.points = points;
            targetInfo = new GlobalTargetInfo(IntVec3.Zero, targetMap);
            targetInfo.tileInt = targetMap.Tile;                        
            ticksToRaid = Rand.Range(3000, 30000);            
            return true;
        }

        private void DoRaid()
        {
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.faction = comp.parent.Faction;
            parms.points = points;
            IncidentDef incidentDef = IncidentDefOf.RaidEnemy;            
            List<RaidStrategyDef> source = DefDatabase<RaidStrategyDef>.AllDefs.Where((RaidStrategyDef s) => s.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat)).ToList();                            
            parms.raidStrategy = source.RandomElement();
            incidentDef.Worker.TryExecute(parms);
        }

        public void ExposeData()
        {            
            Scribe_TargetInfo.Look(ref targetInfo, "targetInfo");
            Scribe_Values.Look(ref points, "points");
            Scribe_Values.Look(ref ticksToRaid, "ticksToRaid");
        }
    }
}
