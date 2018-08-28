using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
    public class WorkGiver_OverlayStock : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_Shelf shelf = t as Building_Shelf;
            
            LocalTargetInfo target = t;
            if (!pawn.CanReserveAndReach(target, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, false))
                return false;
            
			return (shelf != null) && shelf.InRackMode
										   && shelf.CanOverlayThing(out Thing source, out IntVec3 dest)
										   && pawn.CanReserve(source, 1, -1, null, false);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
            Thing stockSource = null;
            IntVec3 destCell = IntVec3.Invalid;
            Building_Shelf s = t as Building_Shelf;
            if (s.CanOverlayThing(out stockSource, out destCell))
                return new Job(StockingJobDefOf.OverlayThing, t, stockSource, destCell);
            return null;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<SlotGroup> slotGroups = pawn?.Map?.haulDestinationManager?.AllGroupsListForReading;
            if (slotGroups == null)
                yield break;
            for (int i = 0; i < slotGroups.Count; i++)
            {
                Building_Shelf shelf = slotGroups[i].parent as Building_Shelf;
                if (shelf != null)
                    yield return shelf;
            }
        }
    }

	public class WorkGiver_OverlayStock_High : WorkGiver_OverlayStock
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return (t is Building_Shelf shelf)
                && (shelf.OrganizeStockPriority == StockingPriority.High)
                && base.HasJobOnThing(pawn, t, forced);
        }
    }

    public class WorkGiver_OverlayStock_Low : WorkGiver_OverlayStock
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return (t is Building_Shelf shelf)
                && (shelf.OrganizeStockPriority == StockingPriority.Low)
                && base.HasJobOnThing(pawn, t, forced);
        }
    }
}
