using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
//using MoreLinq;

namespace AdvancedStocking
{
	public enum StockingPriority : byte
	{
		None,
		Low,
		High
	}

	public class Building_Shelf : Building_Storage
	{
		private readonly float BASE_COMBINE_WORK = 25f;
		private readonly float BASE_OVERLAY_WORK = 10f;
		static public readonly float MAX_UNHELD_STACKLIMITS_TO_DISPLAY = 10;

		private bool inRackMode = false;
		private bool inForbiddenMode = false;
		private bool inPriorityCyclingMode = false;
		private bool autoOrganizeAfterFilling = false;

		private bool canDowncycle = true;
		private bool canUpcycle = true;
		private bool justCycled = false;

		//Will wrap these into properties eventually
		public StockingPriority FillEmptyStockPriority = StockingPriority.None;
		public StockingPriority OrganizeStockPriority = StockingPriority.None;
		public StockingPriority PushFullStockPriority = StockingPriority.None;

		private ShelfOrganizeModeDef currentOrganizeMode;

		private int overlayLimit = 1;
		private Dictionary<ThingDef, int> stackLimits = new Dictionary<ThingDef, int>();
		private List<ThingDef> stackLimitsExposeHelper1;
		private List<int> stackLimitsExposeHelper2;

		private int maxOverlayLimit = 1;
		private float overstackRatioLimitCached = 1f;
		private Dictionary<ThingDef, int> maxStackLimits = new Dictionary<ThingDef, int>();
        private float heaviestAllowedThingMass;

		public SignalManager filterChangedSignalManager = new SignalManager();
		public SignalManager itemsHeldChangedSignalManager = new SignalManager();

		public bool CanShelfBeStocked {
			get {
				return !this.IsForbidden(Faction.OfPlayer);
			}
		}
		
		public int CurrentOverlaysUsed {
			get {
				return Math.Max(1, slotGroup.CellsList
					.Select(cell => Map.thingGrid.ThingsListAtFast(cell).Count(thing => thing.def.EverStoreable))
					.OrderByDescending(count => count)
					.FirstOrFallback());
			}
		}

		public ShelfOrganizeModeDef CurrentOrganizeMode {
			get { return this.currentOrganizeMode; }
		}

		public bool InForbiddenMode { 
			get { return this.inForbiddenMode; }
			set { this.inForbiddenMode = value; }
		}

		public bool InPriorityCyclingMode {
			get { return this.inPriorityCyclingMode; }
			set { 
				this.inPriorityCyclingMode = value; 
				if (value)
					ResetPriorityCycle ();
			}
		}
		
		public bool InRackMode {
			get { return this.inRackMode; }
			set {
				if (value != this.inRackMode) {
					inRackMode = value;
					if (inRackMode == true)
						this.overlayLimit = CurrentOverlaysUsed;
					RecalcOverlays();
				}
			}
		}
		
		public float MaxStockWeight {
			get { return this.GetStatValue(StockingStatDefOf.MaxStockWeight); }
		}

		public int MaxOverlayLimit {
			get { return this.maxOverlayLimit; }
		}

		public int OverlayLimit {
			get { return this.overlayLimit; }
			set {
				if (value > this.maxOverlayLimit) {
					Log.ErrorOnce("Tried setting " + this + " with " + value
								  + " overlays. Max is " + this.maxOverlayLimit, 3197544);
					value = this.maxOverlayLimit;
				}
				if (value < 1) {
					Log.ErrorOnce("AdvancedStocking.Building_Shelf.OverlayLimit Attempting to set overlay to " + value, 3197545);
					value = 1;
				}
				this.overlayLimit = value;
				RecalcStackLimits();
			}
		}
        
		public bool PawnShouldOrganizeAfterFilling {
			get { return this.autoOrganizeAfterFilling; }
			set { this.autoOrganizeAfterFilling = value; }
		}			

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats {
			get {
				foreach (var entry in base.SpecialDisplayStats)
					yield return entry;

				StatCategoryDef stockingCat = StockingStatCategoryDefOf.Stocking;

                if (InRackMode && MaxOverlayLimit < this.GetStatValue(StockingStatDefOf.MaxOverlayLimit))
                    yield return new StatDrawEntry(stockingCat, "MaxOverlayLimit.Constrained.StatLabel".Translate()
                                        , MaxOverlayLimit.ToString(), 0, "MaxOverlayLimit.Constrained.ReportText".Translate());

				if (InRackMode && MaxOverlayLimit > 1) 
					yield return new StatDrawEntry(stockingCat, "OverlayLimitStatLabel".Translate(), OverlayLimit.ToString(),
												   0, "OverlayLimitStatReportText".Translate());

				IEnumerable<ThingDef> thingDefsToDisplay = null;
				if (settings.filter.AllowedDefCount <= MAX_UNHELD_STACKLIMITS_TO_DISPLAY)
					thingDefsToDisplay = settings.filter.AllowedThingDefs;
				else
					thingDefsToDisplay = slotGroup.HeldThings.Select(thing => thing.def).Distinct();

				int i = -1;
				foreach (var thingDef in thingDefsToDisplay) 
					yield return CreateStacklimitStatEntry(thingDef, i);
			}
		}

		//Methods
		public bool CanCombineAnything()
		{
			return CanCombineThings(out Thing t1, out Thing t2)
                && !Map.reservationManager.IsReservedByAnyoneOf(t1, Faction.OfPlayer)
                       && !Map.reservationManager.IsReservedByAnyoneOf(t2, Faction.OfPlayer);
		}

		// Will be running a lot, using for loops as the loops should be very short (2-3 iterations each)
		public bool CanCombineThings(out Thing source, out Thing dest)
		{
			List<IntVec3> cells = slotGroup.CellsList;
			int cellCount = cells.Count;
			List<List<Thing>> things = new List<List<Thing>>(cellCount);
			for(int i = 0; i < cellCount; i++) 
				things.Add(this.Map.thingGrid.ThingsListAtFast(cells[i]));
			for(int i = 0; i < cellCount; i++) {
				for(int j = 0; j < things[i].Count; j++) { 
					source = things [i] [j];
					if (!source.def.EverStoreable)
						continue;
					for (int k = 0; k < cellCount; k++) {
						for(int l = 0; l < things[k].Count; l++) {
							dest = things[k][l];
							if(source != dest && source.CanStackWith(dest) && (source.stackCount + dest.stackCount) <= stackLimits[dest.def]) 
								return true;
						}
					}
				}
			}
			source = null;
			dest = null;
			return false;
		}

		public bool CanOverlayAnything()
		{
			return InRackMode && CanOverlayThing(out Thing t1, out IntVec3 c1)
                && !Map.reservationManager.IsReservedByAnyoneOf(t1, Faction.OfPlayer)
                       && !Map.reservationManager.IsReservedByAnyoneOf(c1, Faction.OfPlayer);
		}

		//TODO rewrite this with For loops and without linq
		public bool CanOverlayThing(out Thing thing, out IntVec3 destCell)
		{
            int overlaysAllowed = OverlayLimit;
            if (overlaysAllowed < 2) {
                thing = null;
                destCell = IntVec3.Invalid;
                return false;
            }
                
			foreach(IntVec3 cell in slotGroup.CellsList) {
				var things = Map.thingGrid.ThingsListAtFast(cell).Where(t => t.def.EverStoreable);
				if (things.Count() == 1) {
					Thing potential = things.Single ();
					foreach (IntVec3 cell2 in slotGroup.CellsList) {
						if (cell == cell2)
							continue;
						var destThings = Map.thingGrid.ThingsListAtFast (cell2).Where (t => t.def.EverStoreable);
						int count = destThings.Count();
						if (count > 0 && count < overlaysAllowed) {
							thing = potential;
							destCell = cell2;
							return true;
						}
					}
				}
			}
			thing = null;
			destCell = IntVec3.Invalid;
			return false;
		}

		public float CombineWorkNeeded(Thing destStock)
		{
			return BASE_COMBINE_WORK * Mathf.Min((float)destStock.stackCount / (float)destStock.def.stackLimit, 5);
		}

		public void CopyStockSettingsFrom(Building_Shelf other)
		{
			settings.CopyFrom (other.settings);
			InRackMode = other.InRackMode;
			InForbiddenMode = other.InForbiddenMode;
			InPriorityCyclingMode = other.InPriorityCyclingMode;
			PawnShouldOrganizeAfterFilling = other.PawnShouldOrganizeAfterFilling;
			FillEmptyStockPriority = other.FillEmptyStockPriority;
			OrganizeStockPriority = other.OrganizeStockPriority;
			PushFullStockPriority = other.PushFullStockPriority;
			overlayLimit = other.overlayLimit;
			stackLimits = new Dictionary<ThingDef, int>(other.stackLimits);

			RecalcOrganizeMode();
		}

		public StatDrawEntry CreateStacklimitStatEntry(ThingDef thingDef, int displayPriority)
		{
			StatCategoryDef stockingCat = StockingStatCategoryDefOf.Stocking;
			
			int overstackLimit = (int)((float)thingDef.stackLimit * overstackRatioLimitCached);
			int overlays = InRackMode ? OverlayLimit : 1;
			float allowedMassPerThing = MaxStockWeight / overlays;
			int massLimit = (int)(allowedMassPerThing / StockingUtility.cachedThingDefMasses[thingDef]);
		
			return new StatDrawEntry(stockingCat, "StackLimitStat.Label".Translate(thingDef.label)
						, stackLimits[thingDef] + "/" + maxStackLimits[thingDef].ToString()
						, displayPriority, "MaxStackLimitStat.Text"
							.Translate(thingDef.label, thingDef.stackLimit, overstackRatioLimitCached, overstackLimit
								, allowedMassPerThing, massLimit, maxStackLimits[thingDef]));
		}

		public void DowncyclePriority()
		{
			if(this.canDowncycle) {
				this.canUpcycle = true;
				this.canDowncycle = false;
				this.justCycled = true;
				settings.Priority = DecrementStoragePriority (settings.Priority);
				if(InForbiddenMode) 
					foreach(Thing t in this.slotGroup.HeldThings)
						t.SetForbidden(false);
			}
		}

		public override void ExposeData() {
			base.ExposeData ();
			Scribe_Values.Look<bool> (ref this.canDowncycle, "canDowncycle", true);
			Scribe_Values.Look<bool> (ref this.canUpcycle, "canUpcycle", false);
			Scribe_Values.Look<bool> (ref this.inRackMode, "inRackMode", false);
			Scribe_Values.Look<bool> (ref this.inPriorityCyclingMode, "inPriorityCyclingMode", false);
			Scribe_Values.Look<bool> (ref this.autoOrganizeAfterFilling, "autoOrganizeAfterFilling", false);
			Scribe_Values.Look<bool> (ref this.inForbiddenMode, "inForbiddenMode", false);
			Scribe_Values.Look<int>(ref this.overlayLimit, "OverlaysAllowed", 1);
			Scribe_Values.Look<StockingPriority> (ref this.OrganizeStockPriority, "organizeStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.FillEmptyStockPriority, "fillEmptyStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.PushFullStockPriority, "pushfullstockPriority", StockingPriority.None);

			if (Scribe.mode == LoadSaveMode.Saving || Scribe.EnterNode("StackLimits")) {
				if(Scribe.mode != LoadSaveMode.Saving)
					Scribe.ExitNode();
				Scribe_Collections.Look<ThingDef, int>(ref this.stackLimits, "StackLimits", LookMode.Def, LookMode.Value,
								ref this.stackLimitsExposeHelper1, ref this.stackLimitsExposeHelper2);
			}

	/*		//Loaded mod to existing game, stackLimits was nulled by Scribe_Collections.Look
			if (Scribe.mode == LoadSaveMode.PostLoadInit && this.stackLimits == null)
				this.stackLimits = new Dictionary<ThingDef, int>();		*/
		}

		public int GetMaxStackLimit(Thing thing) => GetMaxStackLimit(thing.def);
		
		public int GetMaxStackLimit(ThingDef thingDef) => maxStackLimits.TryGetValue(thingDef, out int value) ? value : 1;
		
		public int GetStackLimit(Thing thing) => GetStackLimit(thing.def);

		public int GetStackLimit(ThingDef thingDef)
		{
			if (stackLimits.TryGetValue(thingDef, out int value))
				return value;
			Log.Warning("AdvancedStocking.GetStackLimit Could not find " + thingDef.LabelCap + " custom stacklimit. Returning 1");
			return 1;
		}
		
		public ThingDef GetSingleThingDefOrNull()
		{
			if(this.settings.filter.AllowedDefCount == 1)
				return this.settings.filter.AllowedThingDefs.First();
			return null;
		}

		public bool HasEmptyCell() {
			return slotGroup.EmptyCells ().Any ();
		}

		public bool IsEmpty() 
		{
			foreach(IntVec3 cell in slotGroup?.CellsList ?? Enumerable.Empty<IntVec3>())
				if(cell.GetThingList(this.Map).Any(t => t.def.EverStoreable))
					return false;
			return true;	
		}

		public bool IsFull()	// Some things can be null due to issues with hooking the StorageSettings Priority setter function
		{
			foreach(IntVec3 cell in slotGroup?.CellsList ?? Enumerable.Empty<IntVec3>())
				if(!cell.GetThingList(this.Map).Any(t => t.def.EverStoreable && t.stackCount >= t.def.stackLimit))
					return false;
			return true;
		}
			
		public virtual void Notify_FilterChanged() {
			RecalcOrganizeMode();

			Signal filterChanged = new Signal("FilterChanged", new object[1] { this });
			filterChangedSignalManager.SendSignal(filterChanged);
		}

		public override void Notify_LostThing (Thing lostItem) {
			if (InPriorityCyclingMode && this.canUpcycle && IsEmpty()) 
				UpcyclePriority();

			Signal hasLostItem = new Signal("HasLostItem", new object[2] { this, lostItem });
			itemsHeldChangedSignalManager.SendSignal(hasLostItem);
		}

		public virtual void Notify_PriorityChanging(StoragePriority newPriority)
		{
			if (!this.justCycled)
				ResetPriorityCycle();
			this.justCycled = false;
		}

		public override void Notify_ReceivedThing (Thing newItem) {
			base.Notify_ReceivedThing (newItem);

			Notify_ReceivedMoreOfAThing(newItem, newItem.stackCount);

			Signal hasNewItem = new Signal("HasNewItem", new object[2] { this, newItem });
			itemsHeldChangedSignalManager.SendSignal(hasNewItem);
		}

		public void Notify_ReceivedMoreOfAThing(Thing thing, int receivedCount)
		{
			if(PawnShouldOrganizeAfterFilling)
				TrySetupAutoOrganizeJob(thing);

			if (InForbiddenMode)
				thing.SetForbidden (true);

			if (InPriorityCyclingMode && this.canDowncycle && IsFull())
				DowncyclePriority();
		}

		public void OverlayThing(Thing thing, IntVec3 destCell)
		{
			thing.Position = destCell;
		}
        
		public float OverlayWorkNeeded(IntVec3 destCell)
		{
			return BASE_OVERLAY_WORK * Mathf.Min(Map.thingGrid.ThingsListAtFast (destCell).Count (t => t.def.EverStoreable), 5);
		}

		public void OverstackThings(Thing sourceStock, Thing destStock)
		{
			destStock.TryAbsorbStack(sourceStock, false);
		}
			
		public void RecalcMaxStockWeight()
		{	//Calculations are all handled inside the Stat
			RecalcStackLimits();
		}

		public void RecalcOrganizeMode()
		{
			currentOrganizeMode = ShelfOrganizeModeDefOf.None;
			foreach (var mode in DefDatabase<ShelfOrganizeModeDef>.AllDefs.OrderBy(m => m.order)) {
				if (this.settings.filter.AllowedDefCount > mode.numAllowedThingDefs && mode.numAllowedThingDefs != -1)
					continue;
				foreach (var thingDef in this.settings.filter.AllowedThingDefs) {
                    if (mode.onlyStackLimitOf1 && thingDef.stackLimit != 1)
                        goto Next_Mode;
					if (mode.disallowedThingDefs?.Contains(thingDef) ?? false)
						goto Next_Mode;
					if (mode.disallowedThingCategories?.Any(cat => cat.DescendantThingDefs.Contains(thingDef)) ?? false)
						goto Next_Mode;
					if ((mode.allowedThingDefs != null || mode.allowedThingCategories != null)
						&& !((mode.allowedThingDefs?.Contains(thingDef) ?? false)
							|| (mode.allowedThingCategories?.Any(cat => cat.DescendantThingDefs.Contains(thingDef)) ?? false)))
						goto Next_Mode;
				}
				currentOrganizeMode = mode;
				break;
			Next_Mode:
				;
			}

		//	Log.Message(this.ToString() + " set organize mode " + currentOrganizeMode.defName);
            if (settings.filter.AllowedThingDefs.Any(thingDef => thingDef != null)) //max requires something present
                this.heaviestAllowedThingMass = settings.filter.AllowedThingDefs
                    .Where(def => def != null)  //If mods are unloaded sometimes ThingDef will be null
                    .Max(def => def.GetStatValueAbstract(StatDefOf.Mass, null));
            else
                this.heaviestAllowedThingMass = 5000f;  //that should be big enough
            RecalcOverlays();
		}

		public void RecalcOverlays()
		{
			bool overlayWasAtMaximum = (OverlayLimit == maxOverlayLimit);
			maxOverlayLimit = Math.Min((int)this.GetStatValue(StockingStatDefOf.MaxOverlayLimit)
                                        , (int)(this.GetStatValue(StockingStatDefOf.MaxStockWeight) 
                                            / this.heaviestAllowedThingMass));
            maxOverlayLimit = Math.Max(maxOverlayLimit, 1); //ensure at least 1                                           
			if (overlayWasAtMaximum || OverlayLimit > maxOverlayLimit)
				OverlayLimit = maxOverlayLimit;
            else
			    RecalcStackLimits();
		}
	
		public void RecalcStackLimits()
		{
            this.overstackRatioLimitCached = this.GetStatValue(StockingStatDefOf.MaxOverstackRatio);
        
			int overlays = InRackMode ? OverlayLimit : 1;
			float allowedMassPerThing = MaxStockWeight / overlays;
            float overstackRatio = AS_Mod.settings.overlaysReduceStacklimit
                                        ? overstackRatioLimitCached / overlays
                                        : overstackRatioLimitCached;
            //Sometimes mod removal or other things can cause a null ThingDef to work its way through
			foreach (var thingDef in settings.filter.AllowedThingDefs.Where(def => def != null)) {
				int overstackLimit = (int)((float)thingDef.stackLimit * overstackRatio); 
				int massLimit = (int)(allowedMassPerThing / StockingUtility.cachedThingDefMasses[thingDef]);
				int newMaxStackLimit = Math.Min(overstackLimit, massLimit);
		
				if (newMaxStackLimit <= 0)
					newMaxStackLimit = 1;   //Eliminates Job errors due to Job.Count == 0

				if (!stackLimits.TryGetValue(thingDef, out int oldStackLimit))
					stackLimits.Add(thingDef, Math.Min(thingDef.stackLimit, newMaxStackLimit));

				if (!maxStackLimits.TryGetValue(thingDef, out int oldMaxStackLimit))
					maxStackLimits.Add(thingDef, newMaxStackLimit);
				else
					maxStackLimits[thingDef] = newMaxStackLimit;

				if (oldStackLimit == oldMaxStackLimit || oldStackLimit > newMaxStackLimit)
					stackLimits[thingDef] = newMaxStackLimit;
			}
		}
		
		private void ResetPriorityCycle() {
			this.canUpcycle = IsFull();
			this.canDowncycle = !this.canUpcycle;
		}

		public void SetStackLimit(ThingDef thingDef, int stackLimit)
		{
			if (!maxStackLimits.TryGetValue(thingDef, out int maxStackLimit)) {
				Log.Error("Attempted to set stackLimit for " + thingDef.label + " on " + this.ToString() + " but its maxStackLimit was not cached");
				return;
			}
			else {
				if (stackLimit > maxStackLimit) {
					Log.Error("Attempted to set stacklimit for " + thingDef.label + " on " + this.ToString()
							+ " with a maxStackLimit of only " + maxStackLimit + ". Clamping");
					stackLimit = maxStackLimit;
				}
			}

			stackLimits[thingDef] = stackLimit;
		}		
		
		//I think there is a bug in the StorageSettings ExposeData function with the parameters to the ThingFilter constructor
		//   as a result I can't simply wrap the action but need to perform it myself as well ...
		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup (map, respawningAfterLoad);
			FieldInfo settingsChangedCallback = typeof(ThingFilter).GetField ("settingsChangedCallback", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo tryNotifyChanged = typeof(StorageSettings).GetMethod ("TryNotifyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			Action newAction = () => {
				this.Notify_FilterChanged ();
				tryNotifyChanged.Invoke(this.settings, new object [0]);
			};
			settingsChangedCallback.SetValue (settings.filter, newAction);
			
			RecalcOrganizeMode();
		}

		public bool TrySetupAutoOrganizeJob(Thing newItem)
		{
			Pawn adjPawn = newItem.CellsAdjacent8WayAndInside()
				.Select<IntVec3, Pawn> (cell => Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.Position == cell))
				.FirstOrDefault (pawn => pawn != null && pawn.IsColonist && 
					(pawn.CurJob?.haulMode ?? HaulMode.Undefined) == HaulMode.ToCellStorage);
				
			if (adjPawn != null) 
				return TrySetupAutoOrganizeJob (adjPawn);
			return false;
		}

		public bool TrySetupAutoOrganizeJob(Pawn pawn)
		{
			var combineGiver = new WorkGiver_CombineStock();
			if (combineGiver.HasJobOnThing(pawn, this, false)) {
				pawn.jobs.StartJob(combineGiver.JobOnThing(pawn, this, false), JobCondition.Succeeded, null, true);
				return true;
			}
			var overlayGiver = new WorkGiver_OverlayStock();
			if (overlayGiver.HasJobOnThing(pawn, this, false)) {
				pawn.jobs.StartJob(overlayGiver.JobOnThing(pawn, this, false), JobCondition.Succeeded, null, true);
				return true;
			}
			return false;
		}
              
		public void UpcyclePriority()
		{
			if(this.canUpcycle) {
				this.canDowncycle = true;
				this.canUpcycle = false;
				this.justCycled = true;
				settings.Priority = IncrementStoragePriority (settings.Priority);
			}
		}

		static StoragePriority DecrementStoragePriority(StoragePriority p) {
			StoragePriority r = StoragePriority.Low;
			switch (p) {
			case StoragePriority.Unstored:
				break;
			case StoragePriority.Low:
				break;
			case StoragePriority.Normal:
				r = StoragePriority.Low;
				break;
			case StoragePriority.Preferred:
				r = StoragePriority.Normal;
				break;
			case StoragePriority.Important:
				r = StoragePriority.Preferred;
				break;
			case StoragePriority.Critical:
				r = StoragePriority.Important;
				break;
			}
			return r;
		}

		static StoragePriority IncrementStoragePriority(StoragePriority p){
			StoragePriority r = StoragePriority.Critical;
			switch (p) {
			case StoragePriority.Unstored:
				break;
			case StoragePriority.Low:
				r = StoragePriority.Normal;
				break;
			case StoragePriority.Normal:
				r = StoragePriority.Preferred;
				break;
			case StoragePriority.Preferred:
				r = StoragePriority.Important;
				break;
			case StoragePriority.Important:
				r = StoragePriority.Critical;
				break;
			case StoragePriority.Critical:
				break;
			}
			return r;
		}
	}
}