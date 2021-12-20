using System;
using HarmonyLib;
using RimWorld.Planet;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_World
    {
        [HarmonyPatch(typeof(World), nameof(World.FillComponents))]
        public static class Harmony_World_FillComponents
        {
            public static void Prefix(World __instance)
            {
                ICacheUtility.DeRegister(__instance);
            }
        }
    }
}

