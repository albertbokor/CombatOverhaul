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
    public class JobGiver_FindOrTakeWeaponAmmo : ThinkNode_JobGiver
    {
        private List<ThingDef> cachedDefs = new List<ThingDef>();

        /// <summary>
        /// The maximum distance to the item.
        /// </summary>
        public int maxDist = 8;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_FindOrTakeWeaponAmmo copy = (JobGiver_FindOrTakeWeaponAmmo)base.DeepCopy(resolve);
            copy.maxDist = maxDist;
            return copy;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.inventory == null || pawn.equipment == null)
            {
                return null;
            }
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
            {
                return null;
            }

            ThingWithComps weapon = pawn.equipment.Primary;
            if (weapon == null)
            {
                weapon = pawn.inventory.innerContainer.First(t => t.def.IsRangedWeapon) as ThingWithComps;
                if(weapon == null)
                {
                    return null;
                }
            }
            CompAmmoUser ammoUser = weapon.TryGetCompFast<CompAmmoUser>();
            if(ammoUser == null || !ammoUser.UseAmmo)
            {
                return null;
            }
            cachedDefs.Clear();
            cachedDefs.AddRange(ammoUser.Props?.ammoSet?.ammoTypes?.Select(a => a.ammo)?.ToList() ?? null);
            if(cachedDefs.Count == 0)
            {
                return null;
            }
            Map map = pawn.Map;
            IntVec3 position = pawn.Position;
            for (int i = 0; i < cachedDefs.Count; i++)
            {
                ThingDef def = cachedDefs[i];
                foreach (Thing thing in position.ThingsByDefInRange(map, def, maxDist))
                {
                    if (thing == null || !thing.Spawned)
                    {
                        continue;
                    }
                    if (!pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some, 1, -1))
                    {
                        continue;
                    }
                    return TakeThing(thing, Mathf.Min(thing.stackCount, 50));
                }
            }
            Faction faction = pawn.Faction;
            foreach (Pawn other in position.PawnsInRange(map, maxDist))
            {
                if (other.Faction == null || faction.HostileTo(other.Faction))
                {
                    continue;
                }
                if (other.inventory == null)
                {
                    continue;
                }
                Thing thing = other.inventory.innerContainer.FirstOrFallback(t => cachedDefs.Contains(t.def));
                if (thing != null)
                {
                    int removeNum = (int) Mathf.Max(thing.stackCount / 2f, 1);
                    if(removeNum == 0)
                    {
                        continue;
                    }
                    if (removeNum == thing.stackCount)
                    {
                        other.inventory.innerContainer.Remove(thing);
                    }
                    else
                    {
                        thing.stackCount -= removeNum;
                        thing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                    }
                    GenSpawn.Spawn(thing, other.Position, map);
                    return TakeThing(thing, removeNum);
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

