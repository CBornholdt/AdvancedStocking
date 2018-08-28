using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public class WorkGiver_FillEmptyStock : WorkGiver_Scanner 
	{
        public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.BuildingArtificial);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Shelf shelf = t as Building_Shelf;

			if (shelf == null || !shelf.HasEmptyCell ())
				return false;
				
			if(!shelf.slotGroup.EmptyCells().Any(cell => 
				pawn.CanReserveAndReach (new LocalTargetInfo(cell), PathEndMode.Touch, pawn.NormalMaxDanger (), 1, -1, null, false)))
				return false;

			LocalTargetInfo target = t;
			if (!pawn.CanReserve (target, 1, -1, null, false)) {
				JobFailReason.Is ("ShelfReserved".Translate ());
				return false;
			}

			foreach(SlotGroup sg in pawn.Map?.haulDestinationManager.AllGroupsListInPriorityOrder ?? Enumerable.Empty<SlotGroup>()) 
				if(sg.Settings.Priority > shelf.settings.Priority 
					|| ((sg.Settings.Priority == shelf.settings.Priority) && (sg.parent is Building_Shelf))
					|| sg.parent == shelf)
					continue;
				else 
					foreach(Thing thing in sg.HeldThings) 
						if((thing.stackCount >= thing.def.stackLimit || forced) && shelf.settings.filter.Allows(thing)
							&& StockingUtility.PawnCanAutomaticallyHaul(pawn, thing, forced, shelf.InForbiddenMode))
							return true;
			if(!JobFailReason.HaveReason)
				JobFailReason.Is("FillEmptyStock_NoItemsFound_JobFailReason".Translate());
			if(JobFailReason.Reason == "ForbiddenLower".Translate())
				JobFailReason.Is("FillEmptyStock_AllItemsFoundAreForbidden_JobFailReason".Translate());
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Shelf shelf = t as Building_Shelf;
			List<Thing> potentials = new List<Thing> ();

			if (shelf == null)
				return null;

			foreach(SlotGroup sg in pawn.Map?.haulDestinationManager.AllGroupsListInPriorityOrder ?? Enumerable.Empty<SlotGroup>()) 
				if(sg.Settings.Priority > shelf.settings.Priority 
					|| ((sg.Settings.Priority == shelf.settings.Priority) && (sg.parent is Building_Shelf)))
					continue;
				else 
					foreach(Thing thing in sg.HeldThings)
						if((thing.stackCount >= thing.def.stackLimit || forced) && shelf.settings.filter.Allows(thing)
							&& StockingUtility.PawnCanAutomaticallyHaul(pawn, thing, forced, shelf.InForbiddenMode))
							potentials.Add(thing);

			int minDist = 200000000;
			float maxPartialFullStackFound = 0f;
			Thing chosenThing = null;
			foreach(Thing thing in potentials) {	//I KNOW THIS IS TERRIBLE, BUT I LOVE IT ANYWAYS
				int dist = (thing.Position - pawn.Position).LengthHorizontalSquared + (thing.Position - shelf.Position).LengthHorizontalSquared;
				if(forced) {
					float part = (float)thing.stackCount/(float)thing.def.stackLimit;
					if(part < maxPartialFullStackFound)
						continue;
					if(part > maxPartialFullStackFound) {
						maxPartialFullStackFound = part;
						chosenThing = thing;
						minDist = dist;
						continue;
					}
				}
				if(dist < minDist) {
					minDist = dist;
					chosenThing = thing;
				}
			}

			if(chosenThing == null)
				return null;
			IntVec3 destEmptyCell = shelf.slotGroup.EmptyCells().FirstOrDefault(cell => 
				pawn.CanReserveAndReach(new LocalTargetInfo(cell), PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, false));
			if(destEmptyCell == default(IntVec3))
				return null;
			Job job = new Job(StockingJobDefOf.FillEmptyStock, chosenThing, destEmptyCell, shelf);
			job.haulOpportunisticDuplicates = true;
			job.haulMode = HaulMode.ToCellStorage;
			job.count = chosenThing.stackCount;
							
			return job;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal (Pawn pawn)
		{
			List<SlotGroup> slotGroups = pawn.Map?.haulDestinationManager?.AllGroupsListForReading;
			if(slotGroups == null)
				yield break;
			for (int i = 0; i < slotGroups.Count; i++) {
				Building_Shelf shelf = slotGroups[i].parent as Building_Shelf;
				if(shelf != null)
					yield return shelf;
			}
		}
	}

	public class WorkGiver_FillEmptyStock_High : WorkGiver_FillEmptyStock
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return (t is Building_Shelf shelf)
                && (shelf.FillEmptyStockPriority == StockingPriority.High)
                && base.HasJobOnThing(pawn, t, forced);
        }
    }

    public class WorkGiver_FillEmptyStock_Low : WorkGiver_FillEmptyStock
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return (t is Building_Shelf shelf)
                && (shelf.FillEmptyStockPriority == StockingPriority.Low)
                && base.HasJobOnThing(pawn, t, forced);
        }
    }
}
