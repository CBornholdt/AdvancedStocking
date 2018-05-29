using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
    public abstract class WorkGiver_OverlayStock : WorkGiver_Scanner
    {
        protected abstract StockingPriority priority();

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
            
			return (shelf != null) && shelf.InStockingMode
										   && (this.priority() == shelf.OrganizeStockPriority)
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
            List<SlotGroup> slotGroups = pawn?.Map?.slotGroupManager?.AllGroupsListForReading;
            if (slotGroups == null)
                yield break;
            for (int i = 0; i < slotGroups.Count; i++)
            {
                Building_Shelf shelf = slotGroups[i].parent as Building_Shelf;
                if (shelf != null && shelf.InStockingMode)
                    yield return shelf;
            }
        }
    }

    public class WorkGiver_OverlayStock_High : WorkGiver_OverlayStock
    {
        protected override StockingPriority priority() => StockingPriority.High;
    }
    
    public class WorkGiver_OverlayStock_Normal : WorkGiver_OverlayStock
    {
        protected override StockingPriority priority() => StockingPriority.Normal;
    }

    public class WorkGiver_OverlayStock_Low : WorkGiver_OverlayStock
    {
        protected override StockingPriority priority() => StockingPriority.Low;
    }
}
