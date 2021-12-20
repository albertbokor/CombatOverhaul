using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Verse.AI;
using System.Xml.Linq;
using System;
using RimWorld;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_PawnUtility
    {
        [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.KnownDangerAt))]
        public static class Harmony_PawnUtility_KnownDangerAt
        {
            private static Pawn lastPawn;
            private static SightTracker.SightReader sightReader;
            private static AvoidanceTracker.AvoidanceReader avoidanceReader;

            public static void Postfix(IntVec3 c, Map map, Pawn forPawn, ref bool __result)
            {
                if (!__result)
                {
                    if (lastPawn != forPawn)
                    {
                        forPawn.GetSightReader(out sightReader);
                        forPawn.GetAvoidanceReader(out avoidanceReader);
                        lastPawn = forPawn;
                    }
                    if (sightReader != null)
                    {
                        if (sightReader.GetEnemies(c) > 0)
                        {
                            __result = true;
                            return;
                        }                        
                        if (avoidanceReader != null && avoidanceReader.GetDanger(c) > 0)
                        {
                            __result = true;
                            return;
                        }
                    }
                }                
            }
        }   
    }
}

