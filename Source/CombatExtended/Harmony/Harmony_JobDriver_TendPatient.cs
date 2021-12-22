using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
	public static class Harmony_JobDriver_TendPatient
	{
		[HarmonyPatch(typeof(JobDriver_TendPatient), nameof(JobDriver_TendPatient.MakeNewToils))]
		public static class Harmony_JobDriver_TendPatient_MakeNewToils
		{
			public static void Prefix(JobDriver_TendPatient __instance)
			{
				Pawn pawn = __instance.pawn;
				if (pawn != null && !(pawn.Faction?.IsPlayerSafe() ?? false) && pawn.RaceProps.Humanlike)
				{
					__instance.pawn.GetSightReader(out SightTracker.SightReader reader);
					if (reader != null)
                    {
						__instance.FailOn(() =>
						{
							return reader.GetVisibility(pawn.Position) > 0.01f;
						});
					}
				}
			}
		}
	}
}

