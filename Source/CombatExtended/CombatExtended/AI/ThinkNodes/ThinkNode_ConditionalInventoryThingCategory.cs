using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_ConditionalInventoryThingCategory : ThinkNode_Conditional
	{
        /// <summary>
        /// The list of item catergories to test for.
        /// </summary>
        public List<ThingCategoryDefCount> categories = new List<ThingCategoryDefCount>();

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalInventoryThingCategory copy = (ThinkNode_ConditionalInventoryThingCategory)base.DeepCopy(resolve);
            copy.categories = categories.ToList();
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
            for (int i = 0; i < categories.Count; i++)
            {
                ThingCategoryDefCount category = categories[i];
                if (pawn.inventory.Count((t) => t.HasThingCategory(category.category)) < category.count)
                {
                    return true;
                }
            }
            return true;
        }
    }
}

