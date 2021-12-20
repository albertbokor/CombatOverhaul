using System;
using HarmonyLib;
using Mono.Security.X509.Extensions;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
	public static class Harmony_TrashUtility
	{	
		[HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.CanTrash))]
		public static class Harmony_TrashUtility_CanTrash
		{
			public static bool Prefix(Pawn pawn, Thing t, ref bool __result)
			{
				if (!SafePosition(pawn, t.Position))
				{
					__result = false;
					return false;
				}				
				return true;
			}
		}

		private static bool SafePosition(Pawn pawn, IntVec3 target)
		{
			pawn.GetSightReader(out var sightReader);
			pawn.GetAvoidanceReader(out var avoidanceReader);
			if (sightReader != null || avoidanceReader != null)
			{
				IntVec3 position = pawn.Position;
				for(int i = 0;i <= 4; i++)
                {
					IntVec3 node = position.LerpTo(target, i / 4f);
					for(int j = 0; j < GenAdj.AdjacentCells.Length; j++)
                    {
						IntVec3 cell = node + GenAdj.AdjacentCells[j];
						if (cell.InBounds(pawn.Map))
						{
							if (sightReader.GetEnemies(cell) > 0)
								return false;
							if (avoidanceReader.GetDanger(cell) > 0)
								return false;
						}
                    }
				}
			}
			return true;
		}		
	}
}

