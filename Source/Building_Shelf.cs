using System;
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

	public class Building_Shelf : Building_Storage
	{
		private bool inStockingMode = false;
		private bool inForbiddenMode = false;
		private bool inPriorityCyclingMode = false;
		private bool autoOrganizeAfterFilling = false;

		private bool canDowncycle = true;
		private bool canUpcycle = true;
		private bool justCycled = false;

		//Will wrap these into properties eventually
		public StockingPriority FillEmptyStockPriority = StockingPriority.Normal;
		public StockingPriority OrganizeStockPriority = StockingPriority.Normal;
		public StockingPriority PushFullStockPriority = StockingPriority.Normal;

		private ShelfOrganizeModeDef currentOrganizeMode;

		private float overlayLimitCached;
		private float overstackRatioLimitCached;
		private float maxWeightLimitCached;

		public bool CanShelfBeStocked {
			get {
				return !this.IsForbidden(Faction.OfPlayer);
			}
		}

		public ShelfOrganizeModeDef CurrentOrganizeMode {
			get { return this.currentOrganizeMode; }
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

		public bool PawnShouldOrganizeAfterFilling {
			get { return this.autoOrganizeAfterFilling; }
			set { this.autoOrganizeAfterFilling = value; }
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats {
			get {
				foreach (var entry in base.SpecialDisplayStats)
					yield return entry;

				StatCategoryDef stockingCat = StockingStatCategoryDefOf.Stocking;
				List<ThingDef> outputThingDefs = new List<ThingDef>();
				foreach (var thing in slotGroup.HeldThings.Where(t => t.stackCount >= t.def.stackLimit && !outputThingDefs.Contains(t.def))) {
					outputThingDefs.Add (thing.def);
					yield return new StatDrawEntry (stockingCat, "OverstackLimitStatLabel".Translate (thing.LabelNoCount), 
						GetOverstackLimit (thing).ToString (), 0, "OverstackLimitStatReportText".Translate (thing.LabelNoCount));
				}
			}
		}	

		//Methods
		public void CacheStats()
		{
			this.overlayLimitCached = this.GetStatValue(StockingStatDefOf.MaxOverlayLimit);
			this.overstackRatioLimitCached = this.GetStatValue(StockingStatDefOf.MaxOverstackRatio);
			this.maxWeightLimitCached = this.GetStatValue (StockingStatDefOf.MaxStockWeight);
		}

		public bool CanCombineAnything()
		{
			return InStockingMode && CanCombineThings(out Thing t1, out Thing t2);
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
					if (!source.def.EverStoreable || source.stackCount > source.def.stackLimit)
						continue;	//Ensure that source is not overstacked already
					for (int k = 0; k < cellCount; k++) {
						for(int l = 0; l < things[k].Count; l++) {
							dest = things[k][l];
							if(source != dest && source.CanStackWith(dest) && (source.stackCount + dest.stackCount) <= GetOverstackLimit(dest)) 
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
			return InStockingMode && CanOverlayThing(out Thing t1, out IntVec3 c1);
		}

		//TODO rewrite this with For loops and without linq
		public bool CanOverlayThing(out Thing thing, out IntVec3 destCell)
		{
			int overlayLimit = GetOverlayLimit ();
			foreach(IntVec3 cell in slotGroup.CellsList) {
				var things = Map.thingGrid.ThingsListAtFast(cell).Where(t => t.def.EverStoreable);
				if (things.Count() == 1) {
					Thing potential = things.Single ();
					foreach (IntVec3 cell2 in slotGroup.CellsList) {
						if (cell == cell2)
							continue;
						var destThings = Map.thingGrid.ThingsListAtFast (cell2).Where (t => t.def.EverStoreable);
						if (destThings.Count() > 0 && destThings.Count() < overlayLimit) {
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
			return 100 * (float)destStock.stackCount / (float)destStock.def.stackLimit;
		}

		public void CopyStockSettingsFrom(Building_Shelf other)
		{
			InStockingMode = other.InStockingMode;
			InForbiddenMode = other.InForbiddenMode;
			InPriorityCyclingMode = other.InPriorityCyclingMode;
			PawnShouldOrganizeAfterFilling = other.PawnShouldOrganizeAfterFilling;
			FillEmptyStockPriority = other.FillEmptyStockPriority;
			OrganizeStockPriority = other.OrganizeStockPriority;
			PushFullStockPriority = other.PushFullStockPriority;

			settings.CopyFrom (other.settings);
			RecalculateCurrentOrganizeMode ();
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
			Scribe_Values.Look<bool> (ref this.autoOrganizeAfterFilling, "autoOrganizeAfterFilling", false);
			Scribe_Values.Look<bool> (ref this.inForbiddenMode, "inForbiddenMode", false);
			Scribe_Values.Look<StockingPriority> (ref this.OrganizeStockPriority, "organizeStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.FillEmptyStockPriority, "fillEmptyStockPriority", StockingPriority.None);
			Scribe_Values.Look<StockingPriority> (ref this.PushFullStockPriority, "pushfullstockPriority", StockingPriority.None);
		}

		public float GetMaxWeightLimit()
		{
			return this.maxWeightLimitCached;
		}

		public int GetOverstackLimit(Thing thing)
		{
			if (this.overstackRatioLimitCached == 1)
				return thing.def.stackLimit;
			return (int) Mathf.Min (this.overstackRatioLimitCached * thing.def.stackLimit,
				this.maxWeightLimitCached / StockingUtility.cachedThingDefMasses[thing.def]);
		}

		public int GetOverlayLimit()
		{
			return (int)this.overlayLimitCached;
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
			RecalculateCurrentOrganizeMode ();
		}

		public override void Notify_LostThing (Thing newItem) {
			if (!InStockingMode)
				return;

		//	Log.Message ("Got item, isFull=" + IsFull() + " isEmpty=" + IsEmpty() + " canDowncycle=" + canDowncycle + " canUpcycle=" + canUpcycle); 

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

		//	Log.Message ("Got item, isFull=" + IsFull() + " isEmpty=" + IsEmpty() + " canDowncycle=" + canDowncycle + " canUpcycle=" + canUpcycle); 

			if (InForbiddenMode)
				newItem.SetForbidden (true);

			if (InPriorityCyclingMode && this.canDowncycle && IsFull())
				DowncyclePriority();
		}

		public void OverlayThing(Thing thing, IntVec3 destCell)
		{
			thing.Position = destCell;
		}

		public float OverlayWorkNeeded(IntVec3 destCell)
		{
			return 20f * Map.thingGrid.ThingsListAtFast (destCell).Where (t => t.def.EverStoreable).Count ();
		}

		public void OverstackThings(Thing sourceStock, Thing destStock)
		{
			destStock.TryAbsorbStack(sourceStock, false);
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
				CacheStats();
				return;
			Next_Mode:
				;
			}
			currentOrganizeMode = ShelfOrganizeModeDefOf.None;
			CacheStats();
		}

		private void ResetPriorityCycle() {
			this.canUpcycle = IsFull();
			this.canDowncycle = !this.canUpcycle;
		}

		public bool TrySetupAutoOrganizeJob(Thing newItem)
		{
			Pawn adjPawn = newItem.CellsAdjacent8WayAndInside()
				.Select<IntVec3, Pawn> (cell => Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.Position == cell))
				.Where (pawn => pawn != null && pawn.IsColonist && 
					(pawn.CurJob?.haulMode ?? HaulMode.Undefined) == HaulMode.ToCellStorage)
				.FirstOrDefault ();
				
			if (adjPawn != null) 
				return TrySetupAutoOrganizeJob (adjPawn);
			return false;
		}

		public bool TrySetupAutoOrganizeJob(Pawn pawn)
		{
			Thing sourceThing;
			Thing destThing;
			if (CanCombineThings (out sourceThing, out destThing)) {
				pawn.jobs.StartJob (new Job (StockingJobDefOf.CombineThings, this, sourceThing, destThing), JobCondition.Succeeded);
				return true;
			}
			IntVec3 destCell;
			if(CanOverlayThing(out sourceThing, out destCell)) {
				pawn.jobs.StartJob (new Job (StockingJobDefOf.OverlayThing, this, sourceThing, destCell), JobCondition.Succeeded);
				return true;
			}
			return false;
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