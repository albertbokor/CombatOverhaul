using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil.Cil;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_JobDriver_FollowClose
    {
        [HarmonyPatch]
        public static class Harmony_JobDriver_FollowClose_MakeNewToils
        {
            public static MethodBase mRandomClosewalkCellNear = AccessTools.Method(typeof(CellFinder), nameof(CellFinder.RandomClosewalkCellNear));
            public static Type tNext;

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(JobDriver_FollowClose), "<MakeNewToils>b__8_0");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for(int i = 0;i < codes.Count; i++)
                {
                    if (!finished)
                    {
                        if (codes[i].OperandIs(mRandomClosewalkCellNear))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_JobDriver_FollowClose_MakeNewToils), nameof(Harmony_JobDriver_FollowClose_MakeNewToils.RandomClosewalkCellNear)));
                            continue;
                        }
                    }
                    yield return codes[i];
                }                
            }

            public static IntVec3 RandomClosewalkCellNear(IntVec3 root, Map map, int radius, Predicate<IntVec3> extraValidator, JobDriver_FollowClose instance)
            {
                //Pawn pawn = instance.pawn;
                //if (pawn.mindState?.duty?.def == DutyDefOf.Escort)
                //{
                //    pawn.GetSightReader(out SightTracker.SightReader sightReader);
                //    //Thing target = instance.TargetA.Thing;
                //    //if (target != null && target is Pawn other && other.pather?.curPath != null)
                //    //{
                //    //    other.pather?.curPath
                //    //}
                //    if (sightReader != null && (sightReader.GetEnemies(root) != 0 || sightReader.GetEnemies(pawn.Position) != 0))
                //    {
                //        radius *= 2;
                //        Vector2 direction = sightReader.GetDirection(root);
                //        //IntVec3 floodCenter = root.LerpTo(root + new IntVec3((int)direction.x, 0, (int)direction.y), 0.5f);
                //        IntVec3 bestCell = IntVec3.Invalid;
                //        float bestCellRating = -1;
                //        pawn.Map.GetComponent<AvoidanceTracker>().TryGetAvoidanceReader(pawn, out AvoidanceTracker.AvoidanceReader avoidanceReader);
                //        pawn.Map.GetComponent<MapCompCE>().Flooder.Flood(root,
                //        (cell, parent, cost) =>
                //        {
                //            float cellRating = 0;
                //            if (GenSight.LineOfSight(cell, root, map, true))
                //                cellRating += 10;
                //            cellRating += (4 - Mathf.Abs(sightReader.GetEnemies(cell) - 4f));
                //            if (avoidanceReader != null)
                //                cellRating -= Mathf.Max(avoidanceReader.GetProximity(cell), 4);
                //            cellRating -= cost / 4;
                //            if (cellRating > bestCellRating)
                //            {
                //                bestCellRating = cellRating;
                //                bestCell = cell;
                //            }
                //            map.debugDrawer.FlashCell(cell, cellRating, $"{Math.Round(cellRating, 1)}", 120);
                //        },
                //        (cell) =>
                //        {
                //            return sightReader.GetVisibility(cell);
                //        }, null, maxDist: radius);
                //        if (bestCellRating >= 0 && bestCell.IsValid)
                //            return bestCell;
                //    }
                //}
                return CellFinder.RandomClosewalkCellNear(root, map, radius, extraValidator);
            }
        }
    }
}

