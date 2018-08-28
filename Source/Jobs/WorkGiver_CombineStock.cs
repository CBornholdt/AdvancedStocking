using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public class WorkGiver_CombineStock : WorkGiver_Scanner 
	{
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
            
			return (shelf != null) && shelf.CanCombineThings(out Thing source, out Thing dest)
								   && (pawn.CanReserve(source, 1, -1, null, false)
				                   		|| (shelf.InForbiddenMode && source.IsForbidden(Faction.OfPlayer)))
								   && (pawn.CanReserve(dest, 1, -1, null, false)
										|| (shelf.InForbiddenMode && dest.IsForbidden(Faction.OfPlayer)));
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Thing stockDest = null;
			Thing stockSource = null;
			IntVec3 destCell = IntVec3.Invalid;
			Building_Shelf s = t as Building_Shelf;
			if (s.CanCombineThings(out stockSource, out stockDest))
				return new Job(StockingJobDefOf.CombineThings, t, stockSource, stockDest);
			return null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal (Pawn pawn)
		{
			List<SlotGroup> slotGroups = pawn?.Map?.haulDestinationManager.AllGroupsListForReading;
			if(slotGroups == null)
				yield break;
			for (int i = 0; i < slotGroups.Count; i++) 
				if(slotGroups[i].parent is Building_Shelf shelf)
					yield return shelf;
		}
	}

	public class WorkGiver_CombineStock_High : WorkGiver_CombineStock 
	{
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return (t is Building_Shelf shelf)
				&& (shelf.OrganizeStockPriority == StockingPriority.High)
				&& base.HasJobOnThing(pawn, t, forced);
		}
	}

	public class WorkGiver_CombineStock_Low : WorkGiver_CombineStock 
	{
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return (t is Building_Shelf shelf)
                && (shelf.OrganizeStockPriority == StockingPriority.Low)
                && base.HasJobOnThing(pawn, t, forced);
        }
	}
}
