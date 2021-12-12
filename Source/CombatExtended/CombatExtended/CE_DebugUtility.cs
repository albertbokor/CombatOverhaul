using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Linq;

namespace CombatExtended
{
    public static class CE_DebugUtility
    {
        // private const int LOSSHADOWRADIUS = 70;
        // private static readonly List<Pair<IntVec3, float>> shadowLosCells = new List<Pair<IntVec3, float>>();

        [CE_DebugTooltip(CE_DebugTooltipType.Map)]
        public static string CellPositionTip(Map map, IntVec3 cell)
        {
            return $"Cell: ({cell.x}, {cell.z})";
        }

        [CE_DebugTooltip(CE_DebugTooltipType.World)]
        public static string TileIndexTip(World world, int tile)
        {
            return $"Tile index: {tile}";
        }             

        [CE_DebugTooltip(CE_DebugTooltipType.Map)]
        public static string CoverRatingHostile(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
                return null;
            SightTracker tracker = map.GetComponent<SightTracker>();            
            string message = $"<color=red>Hostile</color>\n" + tracker.friendly.grid.GetDebugInfoAt(cell) + "\n\n<color=red>Friendly</color>\n" + tracker.hostile.grid.GetDebugInfoAt(cell);
            Pawn pawn = cell.GetFirstPawn(map);
            if (pawn != null)
                message += $"\n<color=red>Pawn Flag</color>\n{Convert.ToString((long)pawn.GetCombatFlags(), 2)}";
            return message;
        }      
    }
}
