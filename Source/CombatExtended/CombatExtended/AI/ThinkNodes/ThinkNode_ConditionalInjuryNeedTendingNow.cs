using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_ConditionalInjuryNeedTendingNow : ThinkNode_Conditional
	{
        /// <summary>
        /// The number of ticks until bleeding out required before the node is satisfied.
        /// </summary>
        public int minTicksToDeath = 30000;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalInjuryNeedTendingNow copy = (ThinkNode_ConditionalInjuryNeedTendingNow) base.DeepCopy(resolve);
            copy.minTicksToDeath = minTicksToDeath;            
            return copy;
        }

        public override bool Satisfied(Pawn pawn)
        {
            if (!(pawn.health?.HasHediffsNeedingTend() ?? false))
                return false;
            if (pawn.RaceProps.IsFlesh)
                return false;
            if (HealthUtility.TicksUntilDeathDueToBloodLoss(pawn) > minTicksToDeath)                            
                return false;            
            return true;
        }
    }
}

