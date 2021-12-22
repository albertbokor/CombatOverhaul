using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_ConditionalInventory : ThinkNode_Conditional
	{
        //public List<ThingDefCount> items;

        public override bool Satisfied(Pawn pawn)
        {
            return false;
        }

        public virtual bool TestItem(Thing item)
        {
            return true;
        }
    }
}

