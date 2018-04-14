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
			foreach(IntVec3 c in slotGroup?.CellsList ?? new List<IntVec3>()) {
				List<Thing> thingsList = slotGroup.parent.Map.thingGrid.ThingsListAtFast(c);
				if (thingsList.Count == 0 || (thingsList.Count == 1 && thingsList[0] == slotGroup.parent))	//If thingsList is null, don't return true ...
					yield return c;
			}
		}
	}
}
