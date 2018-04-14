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
			return (shelf != null) && shelf.InStockingMode && (this.priority() == shelf.OrganizeStockPriority) 
				&& (shelf.CanOverStockThings() || shelf.CanOrganizeAnything());
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Thing stockDest = null;
			Thing stockSource = null;
			IntVec3 destCell = IntVec3.Invalid;
			Building_Shelf s = t as Building_Shelf;
			if(s.CanOverStockThings(out stockSource, out stockDest))
				return new Job (StockJobDefs.OverStockThings, t, stockSource, stockDest);
			if(s.CanOrganizeThing(out stockSource, out destCell))
				return new Job (StockJobDefs.OrganizeThing, t, stockSource, destCell);
			return null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal (Pawn pawn)
		{
			List<SlotGroup> slotGroups = pawn?.Map?.slotGroupManager?.AllGroupsListForReading;
			List<Thing> result = new List<Thing>();
			if(slotGroups == null)
				return null;
			for (int i = 0; i < slotGroups.Count; i++) {
				Building_Shelf s = slotGroups[i].parent as Building_Shelf;
				if(HasJobOnThing(pawn, s, false))
					result.Add(s);
			}
			return result;
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
