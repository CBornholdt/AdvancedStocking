using System;
using Verse;
using System.Reflection;
using Harmony;

namespace AdvancedStocking
{
	public static class TerrainGrid_DoTerrainChangedEffects
	{
		public static void Postfix(TerrainGrid __instance, IntVec3 c)
		{
			Map map = (Map)typeof(TerrainGrid).
				GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

			(map.haulDestinationManager.SlotGroupAt(c)?.parent as Building_Shelf)?.RecalcMaxStockWeight();
		}
	}
}
