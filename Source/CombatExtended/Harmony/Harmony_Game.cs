using System;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_Game
    {
        [HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
        public static class Harmony_Game_ExposeData
        {
            public static void Postfix()
            {
                try
                {
                    CE_Scriber.ExecuteLateScribe();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Late scriber is really broken {er}!!");
                }
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class Harmony_Game_LoadGame
        {
            public static void Postfix()
            {
                try
                {
                    CE_Scriber.Reset();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Late scriber is really broken {er}!!");
                }
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.AddMap))]
        public static class Harmony_Game_AddMap
        {
            public static void Postfix(Map map)
            {
                ICacheUtility.Register(map);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
        public static class Harmony_Game_DeinitAndRemoveMap
        {
            public static void Postfix(Map map)
            {
                ICacheUtility.DeRegister(map);
            }
        }
    }
}

