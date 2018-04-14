using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public enum StockingPriority : byte
	{
		None,
		Low,
		Normal,
		High
	}

	public enum StockingOrganizeMode : byte
	{
		Off,
		SingleItem,
		Apparel,
		Weapons
	}

	public class Building_Shelf : Building_Storage
	{
		private bool inStockingMode = false;
		private bool inSingleThingMode = false;
		private bool inForbiddenMode = false;
		private bool inPriorityCyclingMode = false;
		private bool inOverStockMode = false;
		private bool autoOrganizeAfterFilling = false;

		private bool canDowncycle = true;
		private bool canUpcycle = true;
		private bool justCycled = false;

		private bool isLockedToSingleThing = false;
		private bool isShiftingStock = false;
		protected ThingFilter unlockedFilter;	//Used in PostMake
		protected ThingFilter lockedFilter;		//Used in PostMake
		private ThingDef lockedThingDef;
		private int lockedCycleCtr = 0;
		//private LockedThingCycleStrategy lockedCycleLast;
		private int lockedThingStrategyMatchedCtr = 0;
		private int lockedThingStrategyTickCtr = 0;
		private ThingDef lockedThingSuggestedReplacement;


		//Will wrap these into properties eventually
		public StockingPriority FillEmptyStockPriority = StockingPriority.None;
		public StockingPriority OrganizeStockPriority = StockingPriority.None;
		public StockingPriority PushFullStockPriority = StockingPriority.None;

		private StockingOrganizeMode organizeMode = StockingOrganizeMode.Off;

		//enum LockedThingCycleStrategy { WhenEmpty, WhenLow, ForMuchLargerStock, PeriodicallyLargestStock };

		protected float overstockRatio { get; set; } = 3;

		public bool CanShelfBeStocked {
			get {
				return !this.IsForbidden(Faction.OfPlayer);
			}
		}

		//Properties
		public bool InOverStockMode {
			get { return this.inOverStockMode; }
			set { this.inOverStockMode = value; }
		}

		public bool InSingleThingMode {
			get { return this.inSingleThingMode; }
			set {
				if (this.inSingleThingMode == value)
					return;
				if (!value && isLockedToSingleThing)
					UnlockFromSingleThing ();
				inSingleThingMode = value;
				isShiftingStock = value;
			}
		}

		public bool InStockingMode {
			get { return this.inStockingMode; }
			set {
				this.inStockingMode = value;
			}
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

		public bool IsLockedToSingleThing {
			get { return this.isLockedToSingleThing; }
		}

		public bool IsOrganizingEnabled {
			get { return InOverStockMode || (OrganizeMode != StockingOrganizeMode.Off); }
		}

		public bool PawnShouldOrganizeAfterFilling {
			get { return this.autoOrganizeAfterFilling; }
			set { this.autoOrganizeAfterFilling = value; }
		}

		public StockingOrganizeMode OrganizeMode {
			get { return this.organizeMode; }
			set { this.organizeMode = value; }
		}

		//public LockedThingCycleStrategy LockedCycleStrategy { get; } = LockedThingCycleStrategy.WhenEmpty;
		private bool AllowedToOverlayTheseThings(Thing thing1, Thing thing2)
		{
			switch(OrganizeMode) {
			default:
				return false;
			case StockingOrganizeMode.Off:
				return false;
			case StockingOrganizeMode.SingleItem:
				return thing1.def == thing2.def && thing1.def.stackLimit == 1;
			case StockingOrganizeMode.Apparel:
				return ThingCategoryDefOf.Apparel.ThisAndChildCategoryDefs.Intersect(thing1.def.thingCategories).Any() 
					&& ThingCategoryDefOf.Apparel.ThisAndChildCategoryDefs.Intersect(thing2.def.thingCategories).Any() ;
			case StockingOrganizeMode.Weapons:
				return ThingCategoryDefOf.Weapons.ThisAndChildCategoryDefs.Intersect(thing1.def.thingCategories).Any() 
					&& ThingCategoryDefOf.Weapons.ThisAndChildCategoryDefs.Intersect(thing2.def.thingCategories).Any() ;
			}
		}

		//Methods
		public bool CanOverStockThings()
		{
			return InOverStockMode && CanOverStockThings(out Thing t1, out Thing t2);
		}

		// Will be running a lot, using for loops as the loops should be very short (2-3 iterations each)
		public bool CanOverStockThings(out Thing source, out Thing dest)
		{
			List<IntVec3> cells = slotGroup.CellsList;
			List<List<Thing>> things = new List<List<Thing>>();
			for(int i = 0; i < cells.Count; i++) 
				things.Add(this.Map.thingGrid.ThingsListAtFast(cells[i]));
			for(int i = 0; i < cells.Count; i++) {
				for(int j = 0; j < things[i].Count; j++) { 
					for (int k = 0; k < i; k++) {
						for(int l = 0; l < things[k].Count; l++) {
							Thing a = things[i][j];
							Thing b = things[k][l];
							//Log.Message (string.Format("{0:d} {1:d},{2:d} {6:G} {3:d} {4:d},{5:d} {7:G}", a.stackCount, i, j, b.stackCount, k, l, WithinOverstockLimit(a), WithinOverstockLimit(b)));
							if(a.CanStackWith(b) && a.def.stackLimit != 1 && WithinOverstockLimit(a) && WithinOverstockLimit(b)) {
								source = a;
								dest = b;
								//Log.Message(string.Format("Bingo {0:d} {1:d}", a.stackCount, b.stackCount));
								return true;
							}
						}
					}
				}
			}
			source = null;
			dest = null;
			return false;
		}

		public bool CanOrganizeAnything ()
		{
			return (OrganizeMode != StockingOrganizeMode.Off) && CanOrganizeThing(out Thing thing, out IntVec3 cell);
		}

		public bool CanOrganizeThing(out Thing thing, out IntVec3 destCell)
		{
			foreach(IntVec3 cell in slotGroup.CellsList) {
				var things = Map.thingGrid.ThingsListAtFast(cell).Where(t => t.def.EverStoreable);
				if (things.Count() == 1) {
					Thing potential = things.First ();
					foreach (IntVec3 cell2 in slotGroup.CellsList) {
						if (cell == cell2)
							continue;
						var destThings = Map.thingGrid.ThingsListAtFast (cell).Where (t => t.def.EverStoreable);
						if (WithinOverlayLimit (destThings.Count()) && destThings.All (t => AllowedToOverlayTheseThings (potential, t))) {
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

		public void CheckLockedThingCycle(){
			if ((this.lockedThingStrategyTickCtr++ & 4) == 0)
				return;
		}

		public void CopyStockSettingsFrom(Building_Shelf other)
		{
			InStockingMode = other.InStockingMode;
			InSingleThingMode = other.InSingleThingMode;
			InForbiddenMode = other.InForbiddenMode;
			InPriorityCyclingMode = other.InPriorityCyclingMode;
			InOverStockMode = other.InOverStockMode;
			PawnShouldOrganizeAfterFilling = other.PawnShouldOrganizeAfterFilling;
			OrganizeMode = other.OrganizeMode;
			FillEmptyStockPriority = other.FillEmptyStockPriority;
			OrganizeStockPriority = other.OrganizeStockPriority;
			PushFullStockPriority = other.PushFullStockPriority;

			settings.CopyFrom (other.settings);
		}
			
		public void OverStockThings(Thing sourceStock, Thing destStock)
		{
		/*	List<IntVec3> cells = slotGroup.CellsList;
			if(!cells.Contains(sourceStock.Position) || !cells.Contains(destStock.Position)) {
				Log.Message("Attempted to condense two items that are not both inside Stock. Source = " + sourceStock.Label + " Dest = " + destStock.Label);
				return;
			}*/
			destStock.TryAbsorbStack(sourceStock, false);
		}

		public void OrganizeThing(Thing thing, IntVec3 destCell)
		{
			thing.Position = destCell;
			List<Thing> thingsAtDest = Map.thingGrid.ThingsListAtFast (destCell);

			//Now set the newly placed thing on top of the stack
			Thing tmp = thingsAtDest[thingsAtDest.Count - 1];
			thingsAtDest [thingsAtDest.Count - 1] = thingsAtDest [0];
			thingsAtDest [0] = tmp;
		}

		public virtual float OverStockWorkNeeded(Thing destStock)
		{
			return 100 * (float)destStock.stackCount / (float)destStock.def.stackLimit;
		}

		public virtual float OrganizeWorkNeeded(IntVec3 destCell)
		{
			return 20f * Map.thingGrid.ThingsListAtFast (destCell).Where (t => t.def.EverStoreable).Count ();
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
			Scribe_Values.Look<bool> (ref this.inStockingMode, "inStockingMode", false);
			Scribe_Values.Look<bool> (ref this.inPriorityCyclingMode, "inPriorityCyclingMode", false);
			Scribe_Values.Look<bool> (ref this.inOverStockMode, "inOverStockMode", false);
			Scribe_Values.Look<bool> (ref this.inSingleThingMode, "inSingleThingMode", false);
			Scribe_Values.Look<bool> (ref this.autoOrganizeAfterFilling, "autoOrganizeAfterFilling", false);
			Scribe_Values.Look<bool> (ref this.isShiftingStock, "isShiftingStock", false);
			Scribe_Deep.Look<ThingFilter> (ref this.unlockedFilter, "unlockedFilter", new Action(this.Notify_LockedFilterChanged));
			Scribe_Defs.Look<ThingDef> (ref this.lockedThingDef, "lockedThingDef");
			Scribe_Values.Look<bool> (ref this.inForbiddenMode, "inForbiddenMode", false);
			Scribe_Values.Look<StockingPriority> (ref this.OrganizeStockPriority, "organizeStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.FillEmptyStockPriority, "fillEmptyStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.PushFullStockPriority, "pushfullstockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingOrganizeMode> (ref this.organizeMode, "organizeMode", StockingOrganizeMode.Off);
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
			
		private void LockToSingleThing(ThingDef thingDef) {
			if (!IsLockedToSingleThing) 
				settings.filter = this.lockedFilter;

			this.isLockedToSingleThing = false;	//So that Notify_LockedFilterChanged does nothing
			settings.filter.SetDisallowAll ();
			settings.filter.SetAllow (thingDef, true);
			lockedThingDef = thingDef;

			this.isLockedToSingleThing = true;  //turn back on Notify_LockedFilterChanged
			this.isShiftingStock = false;
			Map.listerHaulables.Notify_SlotGroupChanged(this.GetSlotGroup());
		}

		public virtual void Notify_LockedFilterChanged() {
			if (IsLockedToSingleThing) {
				UnlockFromSingleThing ();
				settings.filter.CopyAllowancesFrom (lockedFilter); // Will propagate Notify to saved action in original filter
			}
		}

		public override void Notify_LostThing (Thing newItem) {
			if (!InStockingMode)
				return;

			Log.Message ("Got item, isFull=" + IsFull() + " isEmpty=" + IsEmpty() + " canDowncycle=" + canDowncycle + " canUpcycle=" + canUpcycle); 

			if (InPriorityCyclingMode && this.canUpcycle && IsEmpty()) 
				UpcyclePriority();
		}

		public virtual void Notify_PriorityChanging(StoragePriority newPriority)
		{
			if (!this.justCycled)
				ResetPriorityCycle();
			this.justCycled = false;
		}

		public override void Notify_ReceivedThing (Thing newItem) {
			base.Notify_ReceivedThing (newItem);

			if (!InStockingMode)
				return;

			Log.Message ("Got item, isFull=" + IsFull() + " isEmpty=" + IsEmpty() + " canDowncycle=" + canDowncycle + " canUpcycle=" + canUpcycle); 

			if (InForbiddenMode)
				newItem.SetForbidden (true);

			if (InSingleThingMode && isShiftingStock)
				LockToSingleThing (newItem.def);

			if (InPriorityCyclingMode && this.canDowncycle && IsFull())
				DowncyclePriority();
		}

		public override void PostMake() {
			base.PostMake ();
			this.unlockedFilter = settings.filter;
			this.lockedFilter = new ThingFilter(new Action(this.Notify_LockedFilterChanged));
		}

		private void ResetPriorityCycle() {
			this.canUpcycle = IsFull();
			this.canDowncycle = !this.canUpcycle;
		}

		public void TrySetupAutoOrganizeJob(Pawn pawn)
		{
			Thing sourceThing;
			Thing destThing;
			if (CanOverStockThings (out sourceThing, out destThing)) {
				pawn.jobs.StartJob (new Job (StockJobDefs.OverStockThings, this, sourceThing, destThing), JobCondition.Succeeded);
				return;
			}
			IntVec3 destCell;
			if(CanOrganizeThing(out sourceThing, out destCell)) {
				pawn.jobs.StartJob (new Job (StockJobDefs.OrganizeThing, this, sourceThing, destCell), JobCondition.Succeeded);
				return;
			}
		}

		public override void TickRare () {
			if (isLockedToSingleThing)
				CheckLockedThingCycle ();
		}

		private void UnlockFromSingleThing () {
			settings.filter = unlockedFilter;
			isLockedToSingleThing = false;
			Map.listerHaulables.Notify_SlotGroupChanged(this.GetSlotGroup());
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
		
		protected virtual bool WithinOverstockLimit(Thing t)
		{
			return t.stackCount >= (t.def.stackLimit * 0.5) && (t.stackCount <= (overstockRatio * t.def.stackLimit));
		}

		protected virtual bool WithinOverlayLimit(int numThings)
		{
			if (numThings < 1)
				return false;
			switch (OrganizeMode) {
			default:
				return false;
			case StockingOrganizeMode.Off:
				return false;
			case StockingOrganizeMode.SingleItem:
				return numThings < 8;
			case StockingOrganizeMode.Weapons:
				return numThings < 6;
			case StockingOrganizeMode.Apparel:
				return numThings < 10;
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