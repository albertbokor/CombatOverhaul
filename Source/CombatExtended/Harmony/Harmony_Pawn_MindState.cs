using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_Pawn_MindState
    {
        [HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.Notify_DamageTaken))]
        public static class Harmony_Pawn_MindState_Notify_DamageTaken
        {
            public static void Prefix(Pawn_MindState __instance)
            {
                if (__instance.pawn.Spawned)                
                    __instance.pawn.Map.GetAvoidanceTracker().Notify_Injury(__instance.pawn, __instance.pawn.Position);                
            }
        }
    }
}

