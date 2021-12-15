using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_BreachingUtility
    {
        [HarmonyPatch(typeof(BreachingUtility), nameof(BreachingUtility.ShouldBreachBuilding))]
        public static class Harmony_BreachingUtility_ShouldBreachBuilding
        {
            public static void Postfix(Thing thing, ref bool __result)
            {
                Map map = thing.Map;
                if((thing is Building building))
                {
                    if (building.Faction != null && building.def.IsDoor || (building.GetRegion()?.IsDoorway ?? false))
                    {
                        __result = true;
                    }
                    else if (building.CanBeSeenOver())
                    {
                        __result = false;
                    }
                    else if(building.def.Fillage == FillCategory.Full && building.def.fillPercent == 1)
                    {                        
                    }
                    __result = __result && thing.Faction != null;
                }
                else
                {
                    __result = false;                    
                }                
            }
        }       
    }
}

