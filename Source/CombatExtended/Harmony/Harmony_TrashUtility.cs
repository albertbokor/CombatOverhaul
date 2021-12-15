using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static CombatExtended.SightTracker;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_TrashUtility
    {
        private static Pawn _pawn;
        private static SightReader sightReader;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;

        [HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.ShouldTrashBuilding), new[] {typeof(Pawn), typeof(Building), typeof(bool) })]
        public static class Harmony_TrashUtility_ShouldTrashBuilding_First
        {
            public static bool Prefix(Pawn pawn, Building b, ref bool __result)
            {
                if (_pawn != pawn || sightReader == null)
                {
                    pawn.Map.GetComponent<SightTracker>().TryGetReader(_pawn = pawn, out sightReader);
                    pawn.Map.GetComponent<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);
                }
                if (!PositionSafe(pawn.Position, 4))
                    return __result = false;
                if (!PositionSafe(b.Position, 4))
                    return __result = false;
                if (!PositionSafe(b.Position.LerpTo(pawn.Position, 0.5f), 2))
                    return __result = false;
                return true;
            }
        }        

        [HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.ShouldTrashPlant))]
        public static class Harmony_TrashUtility_ShouldTrashPlant
        {
            public static bool Prefix(Pawn pawn, Plant p, ref bool __result)
            {
                if (_pawn != pawn || sightReader == null)
                {
                    pawn.Map.GetComponent<SightTracker>().TryGetReader(_pawn = pawn, out sightReader);
                    pawn.Map.GetComponent<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);
                }
                if (!PositionSafe(pawn.Position, 4))                
                    return __result = false;
                if (!PositionSafe(p.Position, 4))
                    return __result = false;
                if (!PositionSafe(p.Position.LerpTo(pawn.Position, 0.5f), 2))
                    return __result = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.TrashJob))]
        public static class Harmony_TrashUtility_TrashJob
        {
            public static bool Prefix(Pawn pawn, Thing t)
            {
                if (_pawn != pawn || sightReader == null)
                {
                    pawn.Map.GetComponent<SightTracker>().TryGetReader(_pawn = pawn, out sightReader);
                    pawn.Map.GetComponent<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);
                }
                if (!PositionSafe(pawn.Position, 4))
                    return false;
                if (!PositionSafe(t.Position, 4))
                    return false;
                if (!PositionSafe(t.Position.LerpTo(pawn.Position, 0.5f), 2))
                    return false;
                return false;
            }
        }

        public static bool PositionSafe(IntVec3 pos, int radius = 6)
        {
            if (sightReader != null)
            {
                for (int i = -radius; i <= radius; i += 2)
                {
                    for (int j = -radius; j <= radius; j += 2)
                    {
                        if (sightReader.GetEnemies(pos + new IntVec3(i, 0, j)) > 0)
                            return false;
                    }
                }
            }
            if (avoidanceReader != null)
            {
                if (avoidanceReader.AnyBullets(pos) || avoidanceReader.AnySmoke(pos))
                    return false;
            }
            return true;
        }      
    }
}

