using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class JobGiver_AIRescue : ThinkNode_JobGiver
    {
        /// <summary>
        /// Max distance to ally.
        /// </summary>
        public int maxDist = 16;
        /// <summary>
        /// Max visibility at destination.
        /// </summary>
        public float maxEnemyVisibility = 2f;
        /// <summary>
        /// Max danger at destination.
        /// </summary>
        public float maxDanger = 0f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AIRescue copy = (JobGiver_AIRescue) base.DeepCopy(resolve);
            copy.maxDist = maxDist;
            copy.maxEnemyVisibility = maxEnemyVisibility;
            copy.maxDanger = maxDanger;
            return copy;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
            {
                return null;
            }
            if (pawn.Faction == null)
            {
                return null;
            }
            if (!pawn.GetSightReader(out SightTracker.SightReader sightReader))
            {
                return null;
            }
            if (pawn.IsColonist && pawn.WorkTagIsDisabled(WorkTags.Caring))
            {
                return null;
            }
            if (!pawn.GetAvoidanceReader(out AvoidanceTracker.AvoidanceReader avoidanceReader))
            {
                return null;
            }
            Faction faction = pawn.Faction;
            Predicate<Thing> validator = (thing) =>
            {
                if(thing is Pawn p && p.Downed && p.Faction == faction && pawn.CanReserve(p) && (p.health?.HasHediffsNeedingTend() ?? false))
                {
                    return sightReader.GetVisibility(p.Position) <= maxEnemyVisibility && avoidanceReader.GetDanger(p.Position) <= maxDanger;
                }
                return false;
            };
            ThingRequest request = ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            TraverseParms traverseParms = TraverseParms.For(pawn, Danger.Unspecified);
            if (GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, request, PathEndMode.InteractionCell, traverseParms, maxDist, validator) is Pawn ally)
            {
                CellFlooder flooder = pawn.Map.GetComponent<MapCompCE>().Flooder;
                float moveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed); 
                float bestCost = sightReader.GetVisibility(ally.Position);
                IntVec3 bestSpot = ally.Position;                                
                flooder.Flood(ally.Position, (cell, _, cost) =>
                {
                    float visibility = sightReader.GetVisibility(cell);
                    if (bestCost > visibility)
                    {
                        bestSpot = cell;
                        bestCost = visibility;
                    }
                }, (cell) =>
                {
                    return sightReader.GetVisibility(cell);
                }, (cell) =>
                {
                    if(sightReader.GetVisibility(cell) > maxEnemyVisibility || avoidanceReader.GetDanger(cell) > maxDanger)
                    {
                        return false;
                    }
                    return true;
                }, maxDist);                
                if (bestSpot == ally.Position)
                {
                    return GetTendJob(ally);
                }
                Job carryJob = GetCarryJob(ally, bestSpot); 
                pawn.jobs.jobQueue.EnqueueFirst(GetTendJob(ally));           
                return carryJob;
            }
            return null;
        }

        private static Job GetTendJob(Pawn ally)
        {
            Job tendJob = JobMaker.MakeJob(JobDefOf.TendPatient, ally);
            tendJob.endAfterTendedOnce = true;
            return tendJob;
        }

        private static Job GetCarryJob(Pawn ally, IntVec3 position)
        {
            Job tendJob = JobMaker.MakeJob(CE_JobDefOf.CarryDownedPawn, ally, position);            
            return tendJob;
        }
    }
}

