using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AdvancedStocking
{
	public static class SlotGroup_AdvancedStockingExt
	{
		public static IEnumerable<IntVec3> EmptyCells(this RimWorld.SlotGroup slotGroup)
		{
			List<IntVec3> cellsList = slotGroup.CellsList;
			for(int i = 0; i < cellsList.Count; i++) {
				List<Thing> thingsList = slotGroup.parent.Map.thingGrid.ThingsListAtFast(cellsList[i]);
				if (thingsList.Count == 0 || (thingsList.Count == 1 && thingsList[0] == slotGroup.parent))	//If thingsList is null, don't return true ...
					yield return cellsList[i];
			}
		}
	}
}
