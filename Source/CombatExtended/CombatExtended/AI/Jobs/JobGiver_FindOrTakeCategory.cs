using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class JobGiver_FindOrTakeCategory : ThinkNode_JobGiver
	{
        private List<ThingDef> cachedDefs;

        /// <summary>
        /// The maximum distance to the item.
        /// </summary>
        public int maxDist = 8;
        /// <summary>
        /// The maximum amount of items to take.
        /// </summary>
        public int maxAmount = 1;
        /// <summary>
        /// The thing category of the item
        /// </summary>
        public ThingCategoryDef itemCategory;       

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_FindOrTakeCategory copy = (JobGiver_FindOrTakeCategory) base.DeepCopy(resolve);            
            copy.maxDist = maxDist;
            copy.maxAmount = maxAmount;
            copy.itemCategory = itemCategory;
            return copy;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (cachedDefs == null)
                cachedDefs = itemCategory.DescendantThingDefs.ToList();
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
                return null;
            if (pawn.inventory == null)
                return null;
            Map map = pawn.Map;
            IntVec3 position = pawn.Position;
            for(int i = 0; i < cachedDefs.Count; i++)
            {
                ThingDef def = cachedDefs[i];
                foreach(Thing thing in position.ThingsByDefInRange(map, def, maxDist))
                {
                    map.debugDrawer.FlashCell(thing.Position);
                    if (thing == null || !thing.Spawned)
                    {
                        continue;
                    }
                    if (!pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Unspecified))
                    {
                        continue;
                    }
                    return TakeThing(thing, Mathf.Min(maxAmount, thing.stackCount));
                }
            }
            Faction faction = pawn.Faction;
            foreach(Pawn other in position.PawnsInRange(map, maxDist))
            {
                if (other.Faction == null || faction.HostileTo(other.Faction))
                {
                    continue;
                }
                if(other.inventory == null)
                {
                    continue;
                }
                Thing thing = other.inventory.innerContainer.FirstOrFallback(t => t.def.thingCategories?.Any(c => c == itemCategory) ?? false);
                if (thing != null)
                {
                    int removedNum = Math.Min(1, thing.stackCount);
                    if (removedNum == thing.stackCount)
                    {
                        other.inventory.innerContainer.Remove(thing);                        
                    }
                    else
                    {
                        thing.stackCount -= removedNum;
                        thing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                    }
                    GenSpawn.Spawn(thing, other.Position, map);
                    return TakeThing(thing, thing.stackCount);
                }
            }
            return null;
        }        

        private static Job TakeThing(Thing thing, int count)
        {
            Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
            job.count = count;
            return job;
        }
    }
}

