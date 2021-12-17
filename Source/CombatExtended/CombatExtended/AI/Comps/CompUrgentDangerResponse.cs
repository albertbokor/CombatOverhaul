using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using Mono.Security.X509.Extensions;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentDangerResponse : ICompTactics
    {             
        private const float FOCUSWEIGHT = 0.4f;
        private const float VISIONWEIGHT = 0.4f;
        private const float HEARINGWEIGHT = 0.2f;
        private const float AWARENESSMIN = 0.5f;

        private bool disabled = false;        
        private int cooldownTick = -1;       
        private UInt64 prevFlags;
        private SightTracker.SightReader sightReader;

        public override int Priority => 1200;
        private float Vision
        {
            get
            {
                if (selPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                {
                    return 0f;
                }
                return selPawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
            }
        }
        private float Focus
        {
            get
            {
                if (selPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
                {
                    return 0f;
                }
                return selPawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            }
        }
        private float Hearing
        {
            get
            {
                if (selPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                {
                    return 0f;
                }
                return selPawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
            }
        }

        public CompUrgentDangerResponse()
        {
        }

        public override void Initialize(CompTacticalManager manager)
        {
            base.Initialize(manager);
            disabled = !selPawn.RaceProps.Humanlike || selPawn.jobs == null || selPawn.RaceProps.intelligence != Intelligence.Humanlike || selPawn.Faction == null;                
        }

        public override void TickShort()
        {
            base.TickShort();
            if (disabled || cooldownTick > GenTicks.TicksGame)            
                return;

            if (!selPawn.Spawned || selPawn.Downed || (selPawn.Faction?.IsPlayerSafe() ?? true))
            {
                cooldownTick = GenTicks.TicksGame + GenTicks.TickLongInterval;
                return;
            }
            if ((sightReader = MapSightReader) == null)
            {
                cooldownTick = GenTicks.TicksGame + GenTicks.TickLongInterval;
                return;
            }
            if (selPawn.jobs.curJob?.def == CE_JobDefOf.ReloadWeapon || selPawn.stances?.curStance is Stance_Warmup)
                return;

            Verb verb = selPawn.GetWeaponVerbWithFallback();
            if (verb == null || verb.IsMeleeAttack)
            {
                cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }            
            float vision = Vision, hearing = Hearing, focus = Focus;
            float awarness = Mathf.Clamp01(vision * VISIONWEIGHT + focus * FOCUSWEIGHT + hearing * HEARINGWEIGHT);

            if (verb.EffectiveRange > 5 && selPawn.jobs.curJob?.def == JobDefOf.Goto && selPawn.jobs.curJob.targetA.Thing is Pawn other && other.Faction != null && other.Faction.HostileTo(selPawn.Faction) && other.jobs.curJob?.def == JobDefOf.AttackMelee)
            {
                selPawn.jobs.StopAll(false, false);          
                cooldownTick = (int)(GenTicks.TicksGame + 30 * (2 - awarness));
                return;
            }

            if (selPawn.mindState != null
                && selPawn.mindState.enemyTarget != null
                && (!(selPawn.mindState.enemyTarget is Pawn e) || !e.Dead || !e.Downed)
                && selPawn.CanAttackEnemyNowFast(selPawn.mindState.enemyTarget, verb, sightReader)
                && TryUrgentResponse(selPawn.mindState.enemyTarget, verb))
            {
                cooldownTick = (int)(GenTicks.TicksGame + 540 * (2 - awarness));
                return;
            }
            IntVec3 position = selPawn.Position;
            UInt64 curFlags = sightReader.GetFlags(position);
            UInt64 prevFlags = this.prevFlags;
            this.prevFlags = curFlags;

            if (curFlags == 0)
            {
                cooldownTick = GenTicks.TicksGame + 60;                
                return;
            }
            Thing nearest = null;
            float distMinSqr = 1e5f;
            foreach (Pawn enemy in position.PawnsInRange(Map, verb.EffectiveRange * awarness).Where(p => !p.Downed && (p.Faction?.HostileTo(selPawn.Faction) ?? false)))
            {
                float distSqr = enemy.Position.DistanceToSquared(position);
                if (distSqr < distMinSqr && ((enemy.GetCombatFlags() & curFlags) == curFlags || GenSight.LineOfSight(position, enemy.Position, Map)))
                {
                    nearest = enemy;
                    distMinSqr = distSqr;
                }
            }
            if(nearest == null)
            {
                cooldownTick = GenTicks.TicksGame + 40;
                return;
            }
            TryUrgentResponse(nearest, verb);
            cooldownTick = (int)(GenTicks.TicksGame + 540 * (2 - awarness));
        }

        private bool TryUrgentResponse(Thing enemy, Verb verb)
        {
            if (Controller.settings.DebugDrawTargetedBy)
            {
                Map.debugDrawer.FlashCell(selPawn.Position, 0.5f, $"R");
                Map.debugDrawer.FlashCell(enemy.Position, 1.0f, $"E");
            }
            Job job;
            if (this.prevFlags == 0)
            {
                job = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Range(250, 1800));
                if (job != null)
                {
                    base.selPawn.jobs.StopAll();
                    base.selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
                return true;
            }
            if (selPawn.mindState != null)
                selPawn.mindState.enemyTarget = enemy;

            Verb enemyVerb = enemy.GetPrimaryVerbWithFallback();            
            float distSqr = selPawn.Position.DistanceToSquared(enemy.Position);
            float a = Mathf.Clamp(verb.verbProps.warmupTime * 0.75f, 0.4f, 0.8f);
            float f;            
            f = 1f - Mathf.Abs(1 - distSqr / (verb.EffectiveRange * verb.EffectiveRange * a * a));
            f = Mathf.Clamp01(f);
            if (Rand.Chance(f) || enemyVerb == null || (enemyVerb.verbProps.warmupTime < verb.verbProps.warmupTime && (!enemyVerb.WarmingUp || !enemyVerb.CurrentTarget.HasThing || enemyVerb.CurrentTarget.Thing != selPawn)) || enemyVerb.IsMeleeAttack)
            {
                job = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: verb.verbProps.warmupTime.SecondsToTicks() + verb.verbProps.burstShotCount * verb.verbProps.ticksBetweenBurstShots + 30, checkOverrideOnExpiry: true);
                if (job != null)
                {
                    base.selPawn.jobs.StopAll();
                    base.selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
            }
            CastPositionRequest newReq = default(CastPositionRequest);
            newReq.caster = selPawn;
            newReq.target = enemy;
            newReq.verb = verb;
            newReq.maxRegions = 3;
            newReq.maxRangeFromCaster = verb.verbProps.warmupTime * selPawn.GetStatValue(StatDefOf.MoveSpeed);
            newReq.wantCoverFromTarget = verb.verbProps.range > 5f;
            if (CastPositionFinder.TryFindCastPosition(newReq, out IntVec3 dest))
            {
                cooldownTick = GenTicks.TicksGame + 240;
                job = JobMaker.MakeJob(JobDefOf.Goto, dest);
                if (job != null)
                {
                    base.selPawn.jobs.StopAll();
                    base.selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    job = JobMaker.MakeJob(JobDefOf.Wait_Combat, dest, expiryInterval: verb.verbProps.warmupTime.SecondsToTicks() + verb.verbProps.burstShotCount * verb.verbProps.ticksBetweenBurstShots + 30, checkOverrideOnExpiry: true);
                    if (job != null)
                    {
                        selPawn.jobs.jobQueue.EnqueueFirst(job);
                        return true;
                    } 
                }
            }            
            return false;
        }     
    }
}