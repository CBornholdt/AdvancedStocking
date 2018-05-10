using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public abstract class WorkGiver_PushFullStock : WorkGiver_Scanner
	{
		protected abstract StockingPriority Priority ();

		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.BuildingArtificial);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Shelf shelf = t as Building_Shelf;

			if (shelf == null || !shelf.InStockingMode || this.Priority () != shelf.PushFullStockPriority)
				return false;
				
			LocalTargetInfo target = t;
			if (!pawn.CanReserveAndReach (target, PathEndMode.Touch, pawn.NormalMaxDanger (), 1, -1, null, false))
				return false;

			List<Thing> potentials = new List<Thing>();

			foreach (Thing thing in shelf.slotGroup.HeldThings)
				if (thing.def.EverHaulable && thing.stackCount >= thing.def.stackLimit)
					potentials.Add (thing);

			foreach(SlotGroup sg in pawn.Map?.slotGroupManager.AllGroupsListInPriorityOrder ?? Enumerable.Empty<SlotGroup>()) 
				if(sg.Settings.Priority < shelf.settings.Priority)
					break;
				else if (((sg.Settings.Priority == shelf.settings.Priority) && !(sg.parent is Building_Shelf))
					|| sg.parent == shelf)
					continue;
				else {
					foreach(Thing haulable in potentials) {
						if(!sg.Settings.AllowedToAccept(haulable))
							continue;
						IntVec3 c = sg.EmptyCells().FirstOrDefault((IntVec3 x) => pawn.CanReserveAndReach(x, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1, 1, null, false));
						if(c != default(IntVec3))
							return true;
					}
				}

			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Shelf shelf = t as Building_Shelf;
			Dictionary<Thing, List<IntVec3> > potentialPlaces = new Dictionary<Thing, List<IntVec3>> ();

			if (shelf == null)
				return null;

			foreach (Thing thing in shelf.slotGroup.HeldThings)
				if (thing.def.EverHaulable && thing.stackCount >= thing.def.stackLimit)
					potentialPlaces.Add (thing, new List<IntVec3> ());

			foreach(SlotGroup sg in pawn.Map?.slotGroupManager.AllGroupsListInPriorityOrder ?? Enumerable.Empty<SlotGroup>()) 
				if(sg.Settings.Priority < shelf.settings.Priority)
					break;
				else if (((sg.Settings.Priority == shelf.settings.Priority) && !(sg.parent is Building_Shelf))
					|| sg.parent == shelf)
					continue;
				else {
					foreach(Thing haulable in potentialPlaces.Keys) {
						if(!sg.Settings.AllowedToAccept(haulable))
							continue;
						IntVec3 c = sg.EmptyCells().FirstOrDefault((IntVec3 x) => pawn.CanReserveAndReach(x, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1, 1, null, false));
						if(c != default(IntVec3))
							potentialPlaces[haulable].Add(c);
					}
				}

			int minDist = 200000000;
			IntVec3 chosenPlace = IntVec3.Invalid;
			foreach(IntVec3 c in potentialPlaces.Values.SelectMany((x) => x)) {
				int dist = c.DistanceToSquared(t.Position);
				if(dist < minDist) {
					minDist = dist;
					chosenPlace = c;
				}
			}

			if(chosenPlace == IntVec3.Invalid)
				return null;

			Thing chosenThing = potentialPlaces.Keys.First((Thing x) => potentialPlaces[x].Contains(chosenPlace));
			Job job = new Job(StockingJobDefOf.PushFullStock, chosenThing, chosenPlace);
			job.haulOpportunisticDuplicates = true;
			job.haulMode = HaulMode.ToCellStorage;
			job.count = chosenThing.stackCount;

			return job;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal (Pawn pawn)
		{
			List<SlotGroup> slotGroups = pawn.Map?.slotGroupManager?.AllGroupsListForReading;
			if(slotGroups == null)
				yield break;
			for (int i = 0; i < slotGroups.Count; i++) {
				Building_Shelf shelf = slotGroups[i].parent as Building_Shelf;
				if(shelf != null )
					yield return shelf;
			}
		}
	}

	public class WorkGiver_PushFullStock_High : WorkGiver_PushFullStock 
	{
		protected override StockingPriority Priority()
		{
			return StockingPriority.High;
		}
	}

	public class WorkGiver_PushFullStock_Normal : WorkGiver_PushFullStock 
	{
		protected override StockingPriority Priority()
		{
			return StockingPriority.Normal;
		}
	}

	public class WorkGiver_PushFullStock_Low : WorkGiver_PushFullStock 
	{
		protected override StockingPriority Priority()
		{
			return StockingPriority.Low;
		}
	}
}

