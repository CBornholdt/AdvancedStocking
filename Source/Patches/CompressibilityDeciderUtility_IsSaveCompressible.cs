using System;
using Verse;
using RimWorld;
using System.Reflection;
using Harmony;

namespace AdvancedStocking
{
	public static class CompressibilityDeciderUtility_IsSaveCompressible
	{
		public static void Postfix(Thing t, ref bool __result)
		{
			if (__result && t.PositionHeld.GetSlotGroup(t.MapHeld)?.parent is Building_Shelf)
				__result = false;
		}
	}
}
