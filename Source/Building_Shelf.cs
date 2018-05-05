﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace AdvancedStocking
{
	public enum StockingPriority : byte
	{
		None,
		Low,
		Normal,
		High
	}

/*	public enum StockingOrganizeMode : byte
	{
		Off,
		SingleItem,
		Apparel,
		Weapons,
		Drugs,
		BodyParts
	}	*/

	public class Building_Shelf : Building_Storage
	{
		private bool inStockingMode = false;
		private bool inForbiddenMode = false;
		private bool inPriorityCyclingMode = false;
		private bool autoOrganizeAfterFilling = false;

		private bool canDowncycle = true;
		private bool canUpcycle = true;
		private bool justCycled = false;

		private bool overlayModeEnabled = false;
		private bool overstackModeEnabled = false;

		//Will wrap these into properties eventually
		public StockingPriority FillEmptyStockPriority = StockingPriority.None;
		public StockingPriority OrganizeStockPriority = StockingPriority.None;
		public StockingPriority PushFullStockPriority = StockingPriority.None;

		private ShelfOrganizeModeDef currentOrganizeMode;

		//enum LockedThingCycleStrategy { WhenEmpty, WhenLow, ForMuchLargerStock, PeriodicallyLargestStock };

		public bool CanShelfBeStocked {
			get {
				return !this.IsForbidden(Faction.OfPlayer);
			}
		}

		public ShelfOrganizeModeDef CurrentOrganizeMode {
			get { return this.currentOrganizeMode; }
		}

		//Properties
		public bool InOverstackMode {
			get { return currentOrganizeMode.allowOverstackMode; }
		}

		public bool InOverlayMode {
			get { return currentOrganizeMode.allowOverlayMode; }
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

		public bool IsOrganizingEnabled {
			get { return InOverstackMode || InOverlayMode; }
		}

		public bool IsOverlayModeEnabled {
			get { return this.overlayModeEnabled; }
			set { 
				if (this.overlayModeEnabled != value) {
					this.overlayModeEnabled = value; 
					RecalculateCurrentOrganizeMode ();
				}
			}
		}

		public bool IsOverstackModeEnabled {
			get { return this.overstackModeEnabled; }
			set { 
				if (this.overstackModeEnabled != value) {
					this.overstackModeEnabled = value; 
					RecalculateCurrentOrganizeMode ();
				}
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
				if (InOverstackMode)
					foreach(var thing in slotGroup.HeldThings.Where(t => t.stackCount >= t.def.stackLimit))
						yield return new StatDrawEntry (stockingCat, "OverstackLimitStatLabel".Translate (thing.Label), 
							GetOverstackLimit(thing).ToString(), 0, "OverstackLimitStatReportText".Translate());
			}
		}	

		//public LockedThingCycleStrategy LockedCycleStrategy { get; } = LockedThingCycleStrategy.WhenEmpty;
		private bool AllowedToOverlayTheseThings(Thing thing1, Thing thing2)
		{
			if (thing1.stackCount > thing1.def.stackLimit || thing2.stackCount > thing2.def.stackLimit)
				return false;	//Protect against overstock and then overlay ...
	/*		switch(CurrentOrganizeMode) {
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
			}	*/
			return true;
		}

		//Methods
		public bool CanOverstackAnything()
		{
			return InOverstackMode && CanOverstackThings(out Thing t1, out Thing t2);
		}

		// Will be running a lot, using for loops as the loops should be very short (2-3 iterations each)
		public bool CanOverstackThings(out Thing source, out Thing dest)
		{
			List<IntVec3> cells = slotGroup.CellsList;
			int cellCount = cells.Count;
			List<List<Thing>> things = new List<List<Thing>>(cellCount);
			for(int i = 0; i < cellCount; i++) 
				things.Add(this.Map.thingGrid.ThingsListAtFast(cells[i]));
			for(int i = 0; i < cellCount; i++) {
				for(int j = 0; j < things[i].Count; j++) { 
					for (int k = 0; k < cellCount; k++) {
						if (i == k)
							continue;
						for(int l = 0; l < things[k].Count; l++) {
							Thing a = things[i][j];
							Thing b = things[k][l];
							//Log.Message (string.Format("{0:d} {1:d},{2:d} {6:G} {3:d} {4:d},{5:d} {7:G}", a.stackCount, i, j, b.stackCount, k, l, WithinOverstockLimit(a), WithinOverstockLimit(b)));
							if(a.CanStackWith(b) && a.stackCount < GetOverstackLimit(a) && b.stackCount < GetOverstackLimit(b)) {
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

		public bool CanOverlayAnything()
		{
			return InOverlayMode && CanOverlayThing(out Thing t1, out IntVec3 c1);
		}

		//TODO rewrite this with For loops and without linq
		public bool CanOverlayThing(out Thing thing, out IntVec3 destCell)
		{
			foreach(IntVec3 cell in slotGroup.CellsList) {
				var things = Map.thingGrid.ThingsListAtFast(cell).Where(t => t.def.EverStoreable);
				if (things.Count() == 1) {
					Thing potential = things.Single ();
					foreach (IntVec3 cell2 in slotGroup.CellsList) {
						if (cell == cell2)
							continue;
						var destThings = Map.thingGrid.ThingsListAtFast (cell2).Where (t => t.def.EverStoreable);
						if (destThings.Count() > 0 && destThings.Count() < GetOverlayLimit()) {
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

		public void CopyStockSettingsFrom(Building_Shelf other)
		{
			InStockingMode = other.InStockingMode;
	//		InSingleThingMode = other.InSingleThingMode;
			InForbiddenMode = other.InForbiddenMode;
			InPriorityCyclingMode = other.InPriorityCyclingMode;
			IsOverstackModeEnabled = other.IsOverstackModeEnabled;
			IsOverlayModeEnabled = other.IsOverlayModeEnabled;
			PawnShouldOrganizeAfterFilling = other.PawnShouldOrganizeAfterFilling;
		//	OrganizeMode = other.OrganizeMode;
			FillEmptyStockPriority = other.FillEmptyStockPriority;
			OrganizeStockPriority = other.OrganizeStockPriority;
			PushFullStockPriority = other.PushFullStockPriority;

			settings.CopyFrom (other.settings);
		}

		public ThingDef GetSingleThingDefOrNull()
		{
			if(this.settings.filter.AllowedDefCount == 1)
				return this.settings.filter.AllowedThingDefs.First();
			return null;
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
			Scribe_Values.Look<bool> (ref this.overstackModeEnabled, "overstackModeEnabled", false);
			Scribe_Values.Look<bool> (ref this.overlayModeEnabled, "overlayModeEnabled", false);
	//		Scribe_Values.Look<bool> (ref this.inSingleThingMode, "inSingleThingMode", false);
			Scribe_Values.Look<bool> (ref this.autoOrganizeAfterFilling, "autoOrganizeAfterFilling", false);
	//		Scribe_Values.Look<bool> (ref this.isShiftingStock, "isShiftingStock", false);
	//		Scribe_Deep.Look<ThingFilter> (ref this.unlockedFilter, "unlockedFilter", new Action(this.Notify_FilterChanged));
	//		Scribe_Defs.Look<ThingDef> (ref this.lockedThingDef, "lockedThingDef");
			Scribe_Values.Look<bool> (ref this.inForbiddenMode, "inForbiddenMode", false);
			Scribe_Values.Look<StockingPriority> (ref this.OrganizeStockPriority, "organizeStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.FillEmptyStockPriority, "fillEmptyStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.PushFullStockPriority, "pushfullstockPriority", StockingPriority.None);
	//		Scribe_Values.Look<StockingOrganizeMode> (ref this.organizeMode, "organizeMode", StockingOrganizeMode.Off);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if(Spawned) {
				text += "\n" + "StockingMode".Translate () + ": " + currentOrganizeMode.LabelCap;
			}
			return text;
		}

		public int GetOverstackLimit(Thing thing)
		{
			return (int) Mathf.Min (this.GetStatValue (StockingStatDefOf.MaxOverstackRatio) * thing.def.stackLimit,
				this.GetStatValue (StockingStatDefOf.MaxStockWeight) / thing.GetStatValue (StatDefOf.Mass));
		}

		public int GetOverlayLimit()
		{
			return (int)this.GetStatValue (StockingStatDefOf.MaxOverlayLimit);
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
		/*	if (!IsLockedToSingleThing) 
				settings.filter = this.lockedFilter;

			this.isLockedToSingleThing = false;	//So that Notify_LockedFilterChanged does nothing
			settings.filter.SetDisallowAll ();
			settings.filter.SetAllow (thingDef, true);
			lockedThingDef = thingDef;

			this.isLockedToSingleThing = true;  //turn back on Notify_LockedFilterChanged
			this.isShiftingStock = false;
			Map.listerHaulables.Notify_SlotGroupChanged(this.GetSlotGroup());	*/
		}
			
		public virtual void Notify_FilterChanged() {
			RecalculateCurrentOrganizeMode ();
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

			if(PawnShouldOrganizeAfterFilling)
				TrySetupAutoOrganizeJob(newItem);

			Log.Message ("Got item, isFull=" + IsFull() + " isEmpty=" + IsEmpty() + " canDowncycle=" + canDowncycle + " canUpcycle=" + canUpcycle); 

			if (InForbiddenMode)
				newItem.SetForbidden (true);

			if (InPriorityCyclingMode && this.canDowncycle && IsFull())
				DowncyclePriority();
		}

		public void OrganizeThing(Thing thing, IntVec3 destCell)
		{
			thing.Position = destCell;
		//	List<Thing> thingsAtDest = Map.thingGrid.ThingsListAtFast (destCell);

			//Now set the newly placed thing on top of the stack  (no longer needed)
		/*	Thing tmp = thingsAtDest[thingsAtDest.Count - 1];
			thingsAtDest [thingsAtDest.Count - 1] = thingsAtDest [0];
			thingsAtDest [0] = tmp;	*/
		}

		public void OverstackThings(Thing sourceStock, Thing destStock)
		{
			destStock.TryAbsorbStack(sourceStock, false);
		}

		public virtual float OverstackWorkNeeded(Thing destStock)
		{
			return 100 * (float)destStock.stackCount / (float)destStock.def.stackLimit;
		}

		public virtual float OrganizeWorkNeeded(IntVec3 destCell)
		{
			return 20f * Map.thingGrid.ThingsListAtFast (destCell).Where (t => t.def.EverStoreable).Count ();
		}

		//I think there is a bug in the StorageSettings ExposeData function with the parameters to the ThingFilter constructor
		//   as a result I can't simply wrap the action but need to perform it myself as well ...
		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup (map, respawningAfterLoad);
			FieldInfo settingsChangedCallback = typeof(ThingFilter).GetField ("settingsChangedCallback", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo tryNotifyChanged = typeof(StorageSettings).GetMethod ("TryNotifyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			Action newAction = () => {
				this.Notify_FilterChanged ();
				tryNotifyChanged .Invoke(this.settings, new object [0]);
			};
			settingsChangedCallback.SetValue (settings.filter, newAction);	
			RecalculateCurrentOrganizeMode ();
		}	

		private void RecalculateCurrentOrganizeMode()
		{
			foreach(var mode in DefDatabase<ShelfOrganizeModeDef>.AllDefs.OrderBy(m => m.order)) {
				if(mode.allowOverlayMode && !this.overlayModeEnabled)
					continue;
				if(mode.allowOverstackMode && !this.overstackModeEnabled)
					continue;
				if(this.settings.filter.AllowedDefCount > mode.numAllowedThingDefs && mode.numAllowedThingDefs != -1)
					continue;
				foreach(var thingDef in this.settings.filter.AllowedThingDefs){
					if(mode.disallowedThingDefs?.Contains(thingDef) ?? false)
						goto Next_Mode;
					if(mode.disallowedThingCategories?.Any(cat => cat.DescendantThingDefs.Contains(thingDef)) ?? false)
						goto Next_Mode;
					if((mode.allowedThingDefs != null || mode.allowedThingCategories != null) 
						&& !((mode.allowedThingDefs?.Contains(thingDef) ?? false)
							|| (mode.allowedThingCategories?.Any(cat => cat.DescendantThingDefs.Contains(thingDef)) ?? false)))
						goto Next_Mode;
				}
				currentOrganizeMode = mode;
				return;
			Next_Mode:
				;
			}
			currentOrganizeMode = ShelfOrganizeModeDefOf.None;
		}

		private void ResetPriorityCycle() {
			this.canUpcycle = IsFull();
			this.canDowncycle = !this.canUpcycle;
		}

		public bool TrySetupAutoOrganizeJob(Thing newItem)
		{
			Pawn adjacentPawn = newItem.CellsAdjacent8WayAndInside()
				.Select<IntVec3, Pawn> (cell => Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.Position == cell))
				.Where (pawn => pawn != null && pawn.IsColonist && 
					(pawn.CurJob?.haulMode ?? HaulMode.Undefined) == HaulMode.ToCellStorage)
				.FirstOrDefault ();

			Log.Message("Would try to autoorganize with " + (adjacentPawn?.Name.ToString() ?? "NOONE"));
			return true;
		//	if (adjPawn != null)
		//		TrySetupAutoOrganizeJob (adjacentPawn);
		}

		public void TrySetupAutoOrganizeJob(Pawn pawn)
		{
			Thing sourceThing;
			Thing destThing;
			if (CanOverstackThings (out sourceThing, out destThing)) {
				pawn.jobs.StartJob (new Job (StockingJobDefOf.OverStockThings, this, sourceThing, destThing), JobCondition.Succeeded);
				return;
			}
			IntVec3 destCell;
			if(CanOverlayThing(out sourceThing, out destCell)) {
				pawn.jobs.StartJob (new Job (StockingJobDefOf.OrganizeThing, this, sourceThing, destCell), JobCondition.Succeeded);
				return;
			}
		}

		public override void TickRare () {
		
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

	/*	public float WeightOnShelfInCell(IntVec3 cell)
		{
			List<Thing> things = Map.thingGrid.ThingsListAtFast (cell);
			float mass = 0;
			int thingsOnShelf = 0;
			for (int i = 0; i < things.Count; i++) {
				Thing thing = things [i];
				if (!thing.def.EverStoreable)
					continue;


				mass += thing.GetStatValue (StatDefOf.Mass);
				thingsOnShelf++;
			}
			if (thingsOnShelf >= 2)
				mass += (thingsOnShelf - 1) * currentOrganizeMode.overlayMassCost;	*/

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