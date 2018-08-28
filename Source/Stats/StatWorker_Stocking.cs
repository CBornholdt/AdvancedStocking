using System;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	public class StatWorker_Stocking : StatWorker
	{
		public override bool ShouldShowFor(StatRequest req)
		{
			ThingDef thingDef = req.Def as ThingDef;
			if (thingDef != null && thingDef.category == ThingCategory.Building
			    && (thingDef.thingClass == typeof(Building_Shelf) || thingDef.thingClass.IsSubclassOf (typeof(Building_Shelf))))
				return true;
			return false;
		}
	}
}

