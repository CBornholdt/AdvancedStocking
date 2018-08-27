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
		{   //If on shelf don't compress
            __result = __result && t.GetShelf() == null;
		}
	}
}
