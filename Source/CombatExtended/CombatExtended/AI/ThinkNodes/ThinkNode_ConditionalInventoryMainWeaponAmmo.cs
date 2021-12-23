using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_ConditionalInventoryMainWeaponAmmo : ThinkNode_Conditional
	{
        private CompAmmoUser compAmmo;
        private CompInventory compInventory;
        /// <summary>
        /// The minimum amount of ammo in inventory.
        /// </summary>
        public int minCount;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalInventoryMainWeaponAmmo copy = (ThinkNode_ConditionalInventoryMainWeaponAmmo) base.DeepCopy(resolve);
            copy.minCount = minCount;
            return copy;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn.inventory == null || pawn.equipment == null)
            {
                return ThinkResult.NoJob;
            }
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
            {
                return ThinkResult.NoJob;
            }
            compInventory = pawn.TryGetCompFast<CompInventory>();
            if (compInventory == null)
            {
                return ThinkResult.NoJob;
            }
            compAmmo = pawn.equipment.Primary?.TryGetCompFast<CompAmmoUser>() ?? null;
            if (compAmmo == null || !compAmmo.UseAmmo)
            {
                return ThinkResult.NoJob;
            }            
            ThinkResult result =  base.TryIssueJobPackage(pawn, jobParams);
            compAmmo = null;
            compInventory = null;
            return result;
        }

        public override bool Satisfied(Pawn pawn)
        {
            int sum = 0;
            foreach(AmmoLink link in compAmmo.Props.ammoSet.ammoTypes)
            {
                sum += compInventory.AmmoCountOfDef(link.ammo);
                if (sum >= minCount)
                {
                    return false;
                }
            }
            return true;
        }        
    }
}

