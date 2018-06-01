using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	public class ITab_Stock : ITab
	{
		private static readonly Vector2 WinSize = new Vector2 (360, 300);
		private static readonly int PriorityButtonWidth = 80;
		private Listing_TreeUIOption listing;

		public override bool IsVisible {
			get {
				return (this.SelObject as Building_Shelf) != null;
			}
		}

		public ITab_Stock ()
		{
			this.size = ITab_Stock.WinSize;
			this.labelKey = "TabStock";
			this.tutorTag = "Stock";
		}

		private void SetupListing(Building_Shelf shelf)
		{
			TreeNode_UIOption_Checkbox stockingEnabledCheckbox = new TreeNode_UIOption_Checkbox ("InStockingMode_label".Translate(), 
				() => shelf.InStockingMode, b => shelf.InStockingMode = b, "InStockingMode_tooltip".Translate());

			stockingEnabledCheckbox.children.Add (new TreeNode_UIOption_Checkbox ("InPriorityCyclingMode_label".Translate(), 
				() => shelf.InPriorityCyclingMode, b => shelf.InPriorityCyclingMode = b, "InPriorityCyclingMode_tooltip".Translate(), false, () => shelf.InStockingMode));
//			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("InSingleThingMode_label".Translate(), 
//				() => shelf.InSingleThingMode, b => shelf.InSingleThingMode = b, "Testing", false, () => shelf.InStockingMode));
			stockingEnabledCheckbox.children.Add (new TreeNode_UIOption_Checkbox ("InForbiddenMode_label".Translate(), 
				() => shelf.InForbiddenMode, b => shelf.InForbiddenMode = b, "InForbiddenMode_tooltip".Translate(), false, () => shelf.InStockingMode));
				
			stockingEnabledCheckbox.children.Add (new TreeNode_UIOption_Checkbox ("AutoOrganizeAfterFilling_label".Translate (),
				() => shelf.PawnShouldOrganizeAfterFilling, b => shelf.PawnShouldOrganizeAfterFilling = b, "AutoOrganizeAfterFilling_tooltip".Translate (), 
				false, () => shelf.InStockingMode));
			TreeNode_UIOption prioritiesSubtree = new TreeNode_UIOption ("StockJobPriorities_label".Translate (), "StockJobPriorities_tooltip".Translate ());
			stockingEnabledCheckbox.children.Add (prioritiesSubtree);

			prioritiesSubtree.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Fill_Empty_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), shelf.FillEmptyStockPriority).Translate(), 
				p => shelf.FillEmptyStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Fill_Empty_Stock_Priority_Tooltip".Translate(), false, () => shelf.InStockingMode));
			prioritiesSubtree.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Organize_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), shelf.OrganizeStockPriority).Translate(), 
				p => shelf.OrganizeStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Organize_Stock_Priority_Tooltip".Translate(), false, () => shelf.InStockingMode));
			prioritiesSubtree.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Push_Full_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), shelf.PushFullStockPriority).Translate(), 
				p => shelf.PushFullStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Push_Full_Stock_Priority_Tooltip".Translate(), false, () => shelf.InStockingMode));

			TreeNode_UIOption stockingLimitsRootNode = new TreeNode_UIOption("StockingLimits.Label".Translate());
			stockingLimitsRootNode.children.Add(new TreeNode_UIOption_Slider("OverlayLimit.Label".Translate(shelf.MaxOverlayLimit)
																			, () => (float) shelf.OverlayLimit
																			, val => shelf.OverlayLimit = (int)val
			                                                                , minGetter: () => 1f
			                                                                , maxGetter: () => shelf.MaxOverlayLimit
			                                                                , roundTo: 1f));

			IEnumerable<ThingDef> thingDefsToDisplay = null;
			if (shelf.settings.filter.AllowedDefCount <= 10)
				thingDefsToDisplay = shelf.settings.filter.AllowedThingDefs;
			else
				thingDefsToDisplay = shelf.slotGroup.HeldThings.Select(thing => thing.def).Distinct();

			foreach (var thingDef in thingDefsToDisplay)
				stockingLimitsRootNode.children.Add(new TreeNode_UIOption_Slider(
													() => "StackLimit.Label".Translate(thingDef.LabelCap, shelf.GetMaxStackLimit(thingDef))
													, () => (float)shelf.GetStackLimit(thingDef)
													, value => shelf.SetStackLimit(thingDef, (int)value)
													, minGetter: () => 0f
													, maxGetter: () => shelf.GetMaxStackLimit(thingDef)
													, roundTo: 1f));
                                               
			this.listing = new Listing_TreeUIOption (new List<TreeNode_UIOption>() { stockingEnabledCheckbox, stockingLimitsRootNode });
		}

		protected override void FillTab() {
			Building_Shelf shelf = this.SelObject as Building_Shelf;
			if (shelf == null)
				return;
			Rect rect = new Rect (0, 30, ITab_Stock.WinSize.x, ITab_Stock.WinSize.y - 30);
			listing.Begin (rect);
			listing.DrawUIOptions ();
			listing.End ();
		}

		public override void OnOpen()
		{
			Building_Shelf shelf = this.SelObject as Building_Shelf;
			if (shelf == null)
				return;
			base.OnOpen();
			SetupListing(shelf);
		}
	}
}

