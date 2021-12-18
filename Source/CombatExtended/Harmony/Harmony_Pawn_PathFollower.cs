using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Verse.AI;
using System.Xml.Linq;
using System;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_Pawn_PathFollower
    {
        //[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.NeedNewPath))]
        //public static class Harmony_Pawn_PathFollower_NeedNewPath
        //{
        //    public static MethodBase mNodesConsumedCount = AccessTools.PropertyGetter(typeof(PawnPath), nameof(PawnPath.NodesConsumedCount));

        //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
        //        List<CodeInstruction> codes = new List<CodeInstruction>();
        //        bool finished = false;
        //        for(int i = 0; i < codes.Count; i++)
        //        {
        //            if (!finished)
        //            {
        //                if (codes[i].OperandIs(75))
        //                {
        //                    finished = true;
        //                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
        //                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Pawn_PathFollower), nameof(GetAdjustedNodesConsumedCountLimit)));
        //                    continue;
        //                }
        //            }
        //            yield return codes[i];
        //        }
        //    }
        //
        //    /// <summary>
        //    /// Intended to increase the frequencey at which the path is refreshed for a pawn.
        //    /// </summary>            
        //    private static int GetAdjustedNodesConsumedCountLimit(Pawn_PathFollower pathFollower)
        //    {                
        //        pathFollower.pawn.GetSightReader(out SightTracker.SightReader reader);
        //        if(reader != null)
        //        {
        //            float enemies;
        //            enemies  = reader.GetEnemies(pathFollower.pawn.Position);
        //            if (enemies > 0)
        //                return 50 - Math.Min((int) (enemies * 5), 25);
        //            enemies = reader.GetEnemies(pathFollower.Destination.Cell);
        //            if (enemies > 0)
        //                return 75 - Math.Min((int) (enemies * 5), 50);
        //        }
        //        return 75;
        //    }
        //}
    }
}

