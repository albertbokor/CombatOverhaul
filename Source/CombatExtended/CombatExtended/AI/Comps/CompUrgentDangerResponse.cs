using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using Mono.Security.X509.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentDangerResponse : ICompTactics
    {
        private static IntVec3[] offsets;

        static CompUrgentDangerResponse()
        {
            offsets = new IntVec3[9];
            int k = 0;
            for(int i = -1; i <= 1; i++)            
                for (int j = -1; j <= 1; j++)
                    offsets[k++] = new IntVec3(i, 0, j);            
        }

        private const int CELLSAHEAD = 3;
        
        private const float FOCUSWEIGHT = 0.4f;
        private const float VISIONWEIGHT = 0.4f;
        private const float HEARINGWEIGHT = 0.2f;
        private const float AWARENESSMIN = 0.5f;

        private bool _disabled = false;
        
        private int _cooldownTick = -1;
        private IntVec3 _lastCell;
       
        private UInt64 prevFlags;

        public override int Priority => 1200;
        private float Vision
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
            }
        }
        private float Focus
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            }
        }
        private float Hearing
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
            }
        }

        public CompUrgentDangerResponse()
        {
        }

        public override void Initialize(Pawn pawn)
        {
            base.Initialize(pawn);
            _disabled = !pawn.RaceProps.Humanlike || pawn.RaceProps.intelligence != Intelligence.Humanlike || pawn.Faction == null;                
        }

        public override void TickShort()
        {
            base.TickShort();
            if (_disabled || _cooldownTick > GenTicks.TicksGame)
            {
                return;
            }
            Pawn pawn = SelPawn;            
            if (_lastCell == pawn.Position)
            {
                _lastCell = IntVec3.Invalid;
                _cooldownTick = GenTicks.TicksGame + 30;
                return;
            }            
            SightTracker.SightReader reader = MapSightReader;            
            if (pawn.Downed || reader == null)
            {
                _lastCell = IntVec3.Invalid;
                _cooldownTick = GenTicks.TicksGame + 1800;
            }
            PawnPath path = pawn.pather?.curPath ?? null;
            if (path == null || !pawn.pather.moving)
            {
                _lastCell = pawn.Position;
                _cooldownTick = GenTicks.TicksGame + 60;
                return;
            }
            if (pawn.Faction == null || pawn.Drafted || (CurrentWeapon?.def.IsMeleeWeapon ?? true))
            {
                _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }
            Verb verb;
            if (pawn.mindState != null && pawn.mindState.enemyTarget != null)
            {
                verb = pawn.equipment.PrimaryEq?.PrimaryVerb ?? null;
                if (verb != null)
                {
                    float range = verb.EffectiveRange;
                    if (range > 2.5f && range * range > SelPawn.Position.DistanceToSquared(pawn.mindState.enemyTarget.Position))
                    {
                        UInt64 selFlags = SelPawn.GetCombatFlags();

                        IntVec3 firstPos = pawn.mindState.enemyTarget.Position;
                        IntVec3 secondPos = SelPawn.Position + firstPos;
                        secondPos.x /= 2;
                        secondPos.z /= 2;
                        if ((reader.GetFriendlyFlags(firstPos) & selFlags) != 0 && (reader.GetFriendlyFlags(secondPos) & selFlags) != 0)
                        {
                            if (verb.CanHitTarget(pawn.mindState.enemyTarget))
                            {
                                Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Range(300, 1800));
                                if (job != null)
                                {
                                    SelPawn.jobs.StopAll();
                                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                }
                                _cooldownTick = GenTicks.TicksGame + 900;
                                return;
                            }
                            _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                            return;
                        }
                    }
                }
            }
            UInt64 curFlags = reader.GetFlags(pawn.Position);
            if(curFlags == 0)
            {                
                _cooldownTick = GenTicks.TicksGame + 60;
                prevFlags = 0;
                return;
            }
            float vision = Vision, hearing = Hearing, focus = Focus;
            float awarness = Mathf.Clamp01(vision * VISIONWEIGHT + focus * FOCUSWEIGHT + hearing * HEARINGWEIGHT);
            if(awarness <= AWARENESSMIN)
            {
                _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }
            if (curFlags == prevFlags && !Rand.Chance(awarness))
            {
                _cooldownTick = GenTicks.TicksGame + 60;
                return;
            }
            IntVec3 position = SelPawn.Position;
            prevFlags = curFlags;

            float searchRadius = reader.GetDirection(position).magnitude + 30 * awarness;
            if (searchRadius <= 4)
            {
                _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }
            Pawn nearest = null;
            float min = 1e5f;
            foreach (Pawn enemy in position.PawnsInRange(Map, searchRadius).Where(p => (p.GetCombatFlags() & prevFlags) != 0))
            {
                float d = enemy.Position.DistanceToSquared(position);
                if (d < min)
                {
                    nearest = enemy;
                    min = d;
                }                
            }
            if (nearest == null)
            {
                _cooldownTick = GenTicks.TicksGame + 80;
                return;
            }
            verb = pawn.equipment.PrimaryEq?.PrimaryVerb ?? null;
            if (verb != null && verb.EffectiveRange * verb.EffectiveRange * 0.25f < min && min > 100)
            {
                Job job = SuppressionUtility.GetRunForCoverJob(SelPawn, nearest.Position);
                if (job != null)
                {
                    SelPawn.jobs.StopAll();
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
                _cooldownTick = GenTicks.TicksGame + 1200;
            }
            else
            {
                Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Range(300, 1800));
                if (job != null)
                {
                    SelPawn.jobs.StopAll();
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
                _cooldownTick = GenTicks.TicksGame + 1200;
            }
        }
    }
}