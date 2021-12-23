﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_ConditionalInventory : ThinkNode_Conditional
	{
        /// <summary>
        /// The list of items that needs to be checked.
        /// </summary>
        public List<ThingDefCount> items;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalInventory copy = (ThinkNode_ConditionalInventory) base.DeepCopy(resolve);
            copy.items = items.ToList();
            return copy;
        }       

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn.inventory == null)
            {
                return ThinkResult.NoJob;
            }
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
            {
                return ThinkResult.NoJob;
            }
            return base.TryIssueJobPackage(pawn, jobParams);
        }

        public override bool Satisfied(Pawn pawn)
        {            
            for(int i = 0;i < items.Count; i++)
            {
                ThingDefCount defCount = items[i];
                if(pawn.inventory.Count(defCount.ThingDef) < defCount.count)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

