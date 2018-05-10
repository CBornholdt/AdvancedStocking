using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public abstract class WorkGiver_OrganizeStock : WorkGiver_Scanner 
	{
		protected abstract StockingPriority priority ();

		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.BuildingArtificial);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Shelf shelf = t as Building_Shelf;

			LocalTargetInfo target = t;
			if (!pawn.CanReserveAndReach (target, PathEndMode.Touch, pawn.NormalMaxDanger (), 1, -1, null, false))
				return false;

			return (shelf != null) && shelf.InStockingMode && (this.priority() == shelf.OrganizeStockPriority) 
				&& (shelf.CanOverstackAnything() || shelf.CanOverlayAnything());
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Thing stockDest = null;
			Thing stockSource = null;
			IntVec3 destCell = IntVec3.Invalid;
			Building_Shelf s = t as Building_Shelf;
			if(s.CanOverstackThings(out stockSource, out stockDest))
				return new Job (StockingJobDefOf.OverStockThings, t, stockSource, stockDest);
			if(s.CanOverlayThing(out stockSource, out destCell))
				return new Job (StockingJobDefOf.OrganizeThing, t, stockSource, destCell);
			return null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal (Pawn pawn)
		{
			List<SlotGroup> slotGroups = pawn?.Map?.slotGroupManager?.AllGroupsListForReading;
			if(slotGroups == null)
				yield break;
			for (int i = 0; i < slotGroups.Count; i++) {
				Building_Shelf shelf = slotGroups[i].parent as Building_Shelf;
				if(shelf != null)
					yield return shelf;
			}
		}
	}

	public class WorkGiver_OrganizeStock_High : WorkGiver_OrganizeStock 
	{
		protected override StockingPriority priority() => StockingPriority.High;
	}

	public class WorkGiver_OrganizeStock_Normal : WorkGiver_OrganizeStock 
	{
		protected override StockingPriority priority() => StockingPriority.Normal;
	}

	public class WorkGiver_OrganizeStock_Low : WorkGiver_OrganizeStock 
	{
		protected override StockingPriority priority() => StockingPriority.Low;
	}
}
