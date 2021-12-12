using System;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompTend : ICompTactics
    {
        private const int COOLDOWN_TEND_JOB = 600;
        private const int COOLDOWN_TEND_JOB_CHECK = 1200;

        private const int BLEEDRATE_MAX_TICKS = 40000;

        private int lastTendJobAt = -1;
        private int lastTendJobCheckedAt = -1;

        public override int Priority => 250;

        public bool TendJobIssuedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastTendJobAt < COOLDOWN_TEND_JOB;
            }
        }

        public bool TendJobCheckedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastTendJobCheckedAt < COOLDOWN_TEND_JOB_CHECK;
            }
        }

        public CompTend()
        {
        }

        public override Job TryGiveTacticalJob()
        {
            if (selPawn.Faction.IsPlayerSafe())
                return null;
            if (TendJobIssuedRecently || TendJobCheckedRecently || selPawn.jobs?.curJob?.def == CE_JobDefOf.TendSelf)
            {
                return null;
            }
            if (!selPawn.RaceProps.Humanlike || !selPawn.health.HasHediffsNeedingTend())
            {
                lastTendJobCheckedAt = GenTicks.TicksGame;
                return null;
            }
            if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                lastTendJobCheckedAt = GenTicks.TicksGame;
                return null;
            }
            if (HealthUtility.TicksUntilDeathDueToBloodLoss(selPawn) > BLEEDRATE_MAX_TICKS)
            {
                lastTendJobCheckedAt = GenTicks.TicksGame;
                return null;
            }
            if (selPawn.WorkTagIsDisabled(WorkTags.Caring))
            {
                lastTendJobCheckedAt = GenTicks.TicksGame;
                return null;
            }
            if (selPawn.Position.PawnsInRange(Map, 45).Any(p => p.HostileTo(selPawn) && !selPawn.HiddingBehindCover(p)))
            {
                lastTendJobCheckedAt = GenTicks.TicksGame - COOLDOWN_TEND_JOB_CHECK / 2;
                return SuppressionUtility.GetRunForCoverJob(selPawn);
            }
            lastTendJobAt = GenTicks.TicksGame;
            Job job = JobMaker.MakeJob(CE_JobDefOf.TendSelf, selPawn);
            job.endAfterTendedOnce = false;
            return job;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastTendJobAt, "lastTendJobAt", -1);
            Scribe_Values.Look(ref lastTendJobCheckedAt, "lastTendJobCheckedAt", -1);
        }
    }
}
