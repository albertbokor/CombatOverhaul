using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.UI;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
    internal static class Harmony_PathFinder_FindPath
    {
        private static Pawn pawn;
        private static Map map;
        private static PathFinder instance;
        private static LightingTracker lightingTracker;        
        private static TurretTracker turretTracker;
        private static AvoidanceTracker avoidanceTracker;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;
        private static SightTracker.SightReader sightReader;        
        private static bool crouching;
        private static bool nightTime;
        private static float visibilityAtDest;        
        private static float counter = 0;

        internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, PathFinderCostTuning tuning, out bool __state)
        {
            __state = false;
            if (traverseParms.pawn != null && traverseParms.pawn.Faction != null && traverseParms.pawn.RaceProps.Humanlike && traverseParms.pawn.RaceProps.intelligence == Intelligence.Humanlike)
            {
                __state = true;
                counter = 0;
                instance = __instance;
                map = __instance.map;
                pawn = traverseParms.pawn;
                //Verb verb = pawn.GetWeaponVerbWithFallback();                
                nightTime = map.IsNightTime();                
                lightingTracker = map.GetLightingTracker();
                avoidanceTracker = pawn.Map.GetAvoidanceTracker();
                if (!lightingTracker.IsNight)
                    lightingTracker = null;
                if (map.ParentFaction != pawn?.Faction)
                    turretTracker = map.GetComponent<TurretTracker>();

                SightTracker tracker = map.GetSightTracker();
                pawn.GetSightReader(out sightReader);                
                avoidanceTracker.TryGetReader(pawn, out avoidanceReader);
                if (sightReader != null)
                {
                    visibilityAtDest = sightReader.GetVisibility(dest.Cell) / 2f;
                    if (pawn.GetWeaponVerbWithFallback()?.EffectiveRange < 20)
                        visibilityAtDest *= 2f / 3f;
                }

                // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
                CompSuppressable comp = pawn?.TryGetComp<CompSuppressable>();
                if (comp == null
                    || !comp.isSuppressed
                    || comp.IsCrouchWalking
                    || pawn.CurJob?.def == CE_JobDefOf.RunForCover
                    || start == dest.Cell && peMode == PathEndMode.OnCell)
                {
                    crouching = comp?.IsCrouchWalking ?? false;
                    return true;
                }

                // Make all destinations unreachable
                __state = false;
                __result = PawnPath.NotFound;
                return false;
            }
            else if(tuning?.custom is BreachingGrid.CustomTuning)
            {
                __state = true;
                counter = 0;
                instance = __instance;
                map = __instance.map;
                nightTime = map.IsNightTime();
                lightingTracker = map.GetLightingTracker();
                avoidanceTracker = map.GetAvoidanceTracker();
                if (!lightingTracker.IsNight)
                    lightingTracker = null;
                if (map.ParentFaction != pawn?.Faction)
                    turretTracker = map.GetComponent<TurretTracker>();
                SightTracker tracker = map.GetSightTracker();
                tracker.TryGetEnemyReader(out sightReader);
                avoidanceTracker.TryGetEnemyAvoidanceReader(out avoidanceReader);
                return true;
            }
            else
            {
                Reset();
                return true;
            }
        }

        public static void Postfix(PathFinder __instance, PawnPath __result, bool __state)
        {
            if (avoidanceTracker != null && pawn != null)            
                avoidanceTracker.Notify_PathFound(pawn, __result);            
            Reset();
        }

        public static void Reset()
        {
            avoidanceTracker = null;
            avoidanceReader = null;
            counter = 0;
            instance = null;
            sightReader = null;
            turretTracker = null;
            visibilityAtDest = 0f;
            map = null;
            turretTracker = null;            
            pawn = null;            
            lightingTracker = null;
        }

        /*
         * Search for the vairable that is initialized by the value from the avoid grid or search for
         * ((i > 3) ? num9 : num8) + num15;
         *          
         */
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            bool finished = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!finished)
                {
                    if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder builder1 && builder1.LocalIndex == 46)
                    {
                        finished = true;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 43).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 3);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 29);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PathFinder_FindPath), nameof(Harmony_PathFinder_FindPath.GetCostOffsetAt)));
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
                yield return codes[i];
            }
            if (finished)
                Log.Message("CE: Patched pather!");
        }

        private static int GetCostOffsetAt(int index, int parentIndex, int openNum)
        {
            if (map != null)
            {
                var value = 0;
                if (sightReader != null)
                {                    
                    var visibility = sightReader.GetVisibility(index);
                    if (visibility > visibilityAtDest)
                        value += (int)Mathf.Min(visibility * 45, 600);
                }
                if (value > 0)
                {
                    if (avoidanceReader != null)
                        value += (int)(avoidanceReader.GetPathing(index) * 35);
                    if (nightTime && lightingTracker != null)
                        value += (int)(lightingTracker.CombatGlowAt(map.cellIndices.IndexToCell(index)) * 25f);
                }
                else
                {
                    if (avoidanceReader != null)
                        value += (int)(avoidanceReader.GetPathing(index) * 20);
                }
                if (value > 10f)
                {
                    counter++;
                    //
                    // TODO make this into a maxcost -= something system                    
                    var l1 = 350 * (1f - Mathf.Lerp(0.0f, 0.85f, counter / (openNum + 1f))) * (1f - Mathf.Min(openNum, 7500f) / 10000f);                    
                    var l2 = 150 * (1f - Mathf.Clamp01(PathFinder.calcGrid[parentIndex].knownCost / 4000));                    
                    // we use this so the game doesn't die                
                    return (int)Mathf.Min(value, l1 + l2);
                }
            }            
            return 0;
        }       
    }
}