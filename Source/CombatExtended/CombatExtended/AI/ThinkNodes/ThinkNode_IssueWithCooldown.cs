using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_IssueWithCooldown : ThinkNode_Priority
	{
        /// <summary>
        /// The number of ticks between for cooldown.
        /// </summary>
        public int cooldownTicks = 250;
        /// <summary>
        /// If the cooldown should only start when a job is successfuly issued.
        /// </summary>
        public bool onSuccessOnly = true;
        /// <summary>
        /// Wether to throttle when the performance is low.
        /// </summary>
        public bool thottleIfTpsLow = false;

        /// <summary>
        /// The number of ticks needed between updates.
        /// </summary>
        private int CooldownTicks
        {
            get => thottleIfTpsLow ? (int)(cooldownTicks * (2f - PerformanceTracker.TpsLevel)) : cooldownTicks;
        }
        /// <summary>
        /// Wether the cooldown should start only when a job was issued successfuly
        /// </summary>
        private bool OnSuccessOnly
        {
            get => (!thottleIfTpsLow || !PerformanceTracker.TpsCriticallyLow) && onSuccessOnly;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn.mindState == null)
            {
                return ThinkResult.NoJob;
            }
            if (GenTicks.TicksGame - GetCooldown(pawn) <= CooldownTicks)
            {               
                return ThinkResult.NoJob;
            }
            ThinkResult result = base.TryIssueJobPackage(pawn, jobParams);
            if (result.IsValid || !OnSuccessOnly)
            {
                SetCooldown(pawn);                
            }
            return result;
        }

        /// <summary>
        /// Get the tick at which the cooldown started.
        /// </summary>
        /// <param name="pawn">pawn</param>
        /// <returns>the tick when cooldown started</returns>
        private int GetCooldown(Pawn pawn)
        {
            if (pawn.mindState.thinkData.TryGetValue(base.UniqueSaveKey, out int val))
            {
                return val;
            }            
            return -1;
        }

        /// <summary>
        /// set the cooldown starting tick.
        /// </summary>
        /// <param name="pawn"></param>
        private void SetCooldown(Pawn pawn)
        {
            pawn.mindState.thinkData[base.UniqueSaveKey] = GenTicks.TicksGame;
        }
    }
}

