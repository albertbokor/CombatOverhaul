using System;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_ThingWithComps
    {
        [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps))]
        public static class Harmony_ThingWithComps_InitializeComps
        {
            public static void Postfix(ThingWithComps __instance)
            {
                ICacheUtility.Register(__instance);
            }
        }

        [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.Destroy))]
        public static class Harmony_ThingWithComps_
        {
            public static void Prefix(ThingWithComps __instance)
            {
                ICacheUtility.DeRegister(__instance);
            }
        }
    }
}

