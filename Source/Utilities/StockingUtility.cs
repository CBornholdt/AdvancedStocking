using System;
using RimWorld;
using Verse.AI;
using Verse;
using System.Reflection;
using System.Collections.Generic;

namespace AdvancedStocking
{
	public static class StockingUtility
	{
		//Populated by StockingGameComponent
		public static Dictionary<ThingDef, float> cachedThingDefMasses;

		public static bool PawnCanAutomaticallyHaul (Pawn p, Thing t, bool forced, bool ignoreForbidden)
		{
			if (!ignoreForbidden)
				return HaulAIUtility.PawnCanAutomaticallyHaul (p, t, forced);

			MethodInfo basicChecks = typeof(HaulAIUtility).GetMethod ("PawnCanAutomaticallyHaulBasicChecks", BindingFlags.NonPublic | BindingFlags.Static);
			return t.def.EverHaulable 
				&& (t.def.alwaysHaulable || t.Map.designationManager.DesignationOn (t, DesignationDefOf.Haul) != null 
					|| t.IsInValidStorage ()) 
				&& (bool)basicChecks.Invoke(null, new object[3] {p, t, forced});
		}
	}
}

