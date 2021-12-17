using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_CoverGrid
    {
        public static WallGrid grid;        

        public static MethodBase mCellToIndex = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(IntVec3) });
        public static MethodBase mCellToIndex2 = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(int), typeof(int) });

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.Register))]
        public static class Harmony_CoverGrid_Register
        {
            public static void Prefix(CoverGrid __instance, Thing t)
            {                
                if (t.def.fillPercent > 0)                
                    grid = __instance.map.GetComponent<WallGrid>();                
            }

            public static void Postfix()
            {
                grid = null;                
            }
        }

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.DeRegister))]
        public static class Harmony_CoverGrid_DeRegister
        {
            public static void Prefix(CoverGrid __instance, Thing t)
            {                
                if (t.def.fillPercent > 0)                
                    grid = __instance.map.GetComponent<WallGrid>();                                    
            }

            public static void Postfix()
            {
                grid = null;                
            }            
        }

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.RecalculateCell))]
        public static class Harmony_CoverGrid_RecalculateCell
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < codes.Count; i++)
                {                    
                    if (!finished)
                    {
                        if (codes[i].opcode == OpCodes.Ret)
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
                            yield return new CodeInstruction(OpCodes.Ldloc_0);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_CoverGrid), nameof(Harmony_CoverGrid.Set)));
                        }
                    }
                    yield return codes[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(IntVec3 cell, Thing t)
        {
            if (grid != null)
            {
                if (t == null)
                    grid[cell] = 0;
                else if(t is Building ed && ed.def.Fillage == FillCategory.Full) // checks for buildings. redundent but doesn't do any harm.
                    grid[cell] = 1.0f;
                else if (t.def.category == ThingCategory.Plant)
                    grid[cell] = t.def.fillPercent * 0.5f;
                else
                    grid[cell] = t.def.fillPercent;
            }
        }     
    }
}

