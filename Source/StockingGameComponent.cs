using System;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace AdvancedStocking
{
	//This simply pushes the stackLimit check until after everything has loaded ...
	public class StockingGameComponent : GameComponent
	{
		private List<Thing> thingsToCheck;

		public StockingGameComponent(Game game) 
		{
			thingsToCheck = new List<Thing> ();
			StockingUtility.cachedThingDefMasses = new Dictionary<ThingDef, float> ();
			foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.EverStorable(willMinifyIfPossible: false)))
				StockingUtility.cachedThingDefMasses.Add (thingDef, thingDef.GetStatValueAbstract (StatDefOf.Mass));
		}

		public void AddThingForOverstackCheck(Thing thing)
		{
			thingsToCheck.Add (thing);
		}

		public override void LoadedGame ()
		{
			foreach (var thing in thingsToCheck) {
				var slotGroup = thing.Map.haulDestinationManager.SlotGroupAt (thing.Position);
				if (slotGroup != null && slotGroup.parent != null && slotGroup.parent is Building_Shelf)
					continue;
				Log.Error (string.Concat (new object[] {
					"Spawned ",
					thing,
					" with stackCount ",
					thing.stackCount,
					" but stackLimit is ",
					thing.def.stackLimit,
					". Truncating."
				}));
				thing.stackCount = thing.def.stackLimit;
			}

			thingsToCheck.Clear ();
		}
	}
}

