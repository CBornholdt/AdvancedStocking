using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	public class ITab_Stock : ITab, ISignalReceiver
	{
		private static readonly Vector2 WinSize = new Vector2 (360, 300);
		private static readonly int PriorityButtonWidth = 80;
		private Listing_TreeUIOption listing;
		private Building_Shelf displayingFor;

        public static readonly int ReservationOptionIndex = 2;

        public float CellSpacing => 2;

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
			TreeNode_UIOption advancedStockingRootNode = new TreeNode_UIOption("AdvancedStocking.Label".Translate());

            (new TreeNode_UIOption_Checkbox("InRackMode.Label".Translate(),
                () => shelf.InRackMode, b => shelf.InRackMode = b, "InRackMode.ToolTip".Translate()))
                .AddAsChildTo(advancedStockingRootNode);

			(new TreeNode_UIOption_Checkbox("InPriorityCyclingMode_label".Translate(),
				() => shelf.InPriorityCyclingMode, b => shelf.InPriorityCyclingMode = b, "InPriorityCyclingMode_tooltip".Translate(), false))
                .AddAsChildTo(advancedStockingRootNode);
			//			advancedStockingRootNode.children.Add (new TreeNode_UIOptionCheckbox ("InSingleThingMode_label".Translate(), 
			//				() => shelf.InSingleThingMode, b => shelf.InSingleThingMode = b, "Testing", false, () => shelf.InStockingMode));
			(new TreeNode_UIOption_Checkbox("InForbiddenMode_label".Translate(),
				() => shelf.InForbiddenMode, b => shelf.InForbiddenMode = b, "InForbiddenMode_tooltip".Translate(), false))
                .AddAsChildTo(advancedStockingRootNode);

			(new TreeNode_UIOption_Checkbox("AutoOrganizeAfterFilling_label".Translate(),
				() => shelf.PawnShouldOrganizeAfterFilling, b => shelf.PawnShouldOrganizeAfterFilling = b, "AutoOrganizeAfterFilling_tooltip".Translate(),
				false))
                .AddAsChildTo(advancedStockingRootNode);
			TreeNode_UIOption prioritiesSubtree = new TreeNode_UIOption("StockJobPriorities_label".Translate(), "StockJobPriorities_tooltip".Translate());
			prioritiesSubtree.AddAsChildTo(advancedStockingRootNode);

			(new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Fill_Empty_Stock_Priority".Translate(),
				() => Enum.GetName(typeof(StockingPriority), shelf.FillEmptyStockPriority).Translate(),
				p => shelf.FillEmptyStockPriority = p,
				null, ITab_Stock.PriorityButtonWidth, "Fill_Empty_Stock_Priority_Tooltip".Translate(), false))
                .AddAsChildTo(prioritiesSubtree);
			(new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Organize_Stock_Priority".Translate(),
				() => Enum.GetName(typeof(StockingPriority), shelf.OrganizeStockPriority).Translate(),
				p => shelf.OrganizeStockPriority = p,
				null, ITab_Stock.PriorityButtonWidth, "Organize_Stock_Priority_Tooltip".Translate(), false))
                .AddAsChildTo(prioritiesSubtree);
		
			TreeNode_UIOption stockingLimitsRootNode = new TreeNode_UIOption("StockingLimits.Label".Translate());
			if(shelf.MaxOverlayLimit > 1)
				(new TreeNode_UIOption_Slider("OverlayLimit.Label".Translate(shelf.CurrentOverlaysUsed, shelf.MaxOverlayLimit)
												, valGetter: () => (float)shelf.OverlayLimit
												, valSetter: val => shelf.OverlayLimit = (int)val
												, minGetter: () => shelf.CurrentOverlaysUsed
												, maxGetter: () => shelf.MaxOverlayLimit
												, roundTo: 1f
												, toolTip: "OverlayLimit.ToolTip".Translate()))
                    .AddAsChildTo(stockingLimitsRootNode);

			IEnumerable<ThingDef> thingDefsToDisplay = null;
			if (shelf.settings.filter.AllowedDefCount <= Building_Shelf.MAX_UNHELD_STACKLIMITS_TO_DISPLAY)
				thingDefsToDisplay = shelf.settings.filter.AllowedThingDefs;
			else {
				thingDefsToDisplay = shelf.slotGroup.HeldThings.Select(thing => thing.def).Distinct();
			}

			foreach (var thingDef in thingDefsToDisplay)
				(new TreeNode_UIOption_Slider( () => "StackLimit.Label".Translate(thingDef.LabelCap, shelf.GetMaxStackLimit(thingDef))
            									, valGetter: () => (float)shelf.GetStackLimit(thingDef)
            									, valSetter: value => shelf.SetStackLimit(thingDef, (int)value)
            									, minGetter: () => 0f
            									, maxGetter: () => shelf.GetMaxStackLimit(thingDef)
            									, roundTo: 1f
            									, toolTip: "StackLimit.ToolTip".Translate()))
                    .AddAsChildTo(stockingLimitsRootNode);

            if (stockingLimitsRootNode.children.NullOrEmpty())
                stockingLimitsRootNode.label = "StockingLimits.Label.NoChildren".Translate();

			Action<TreeNode, TreeNode> configureNodeOpenings = null;
			FieldInfo openBitsField = typeof(Verse.TreeNode).GetField("openBits", BindingFlags.Instance | BindingFlags.NonPublic);
			configureNodeOpenings = delegate (TreeNode oldNode, TreeNode newNode) {
				openBitsField.SetValue(newNode, openBitsField.GetValue(oldNode));
				IEnumerator<TreeNode> oldNodeEnum = oldNode.children?.GetEnumerator() ?? Enumerable.Empty<TreeNode>().GetEnumerator();
				IEnumerator<TreeNode> newNodeEnum = newNode.children?.GetEnumerator() ?? Enumerable.Empty<TreeNode>().GetEnumerator();
				while (oldNodeEnum.MoveNext() && newNodeEnum.MoveNext())
					configureNodeOpenings(oldNodeEnum.Current, newNodeEnum.Current);
			};

			var newListing = new Listing_TreeUIOption(new List<TreeNode_UIOption>() { stockingLimitsRootNode
                                                                                        , advancedStockingRootNode });
            int maxOptionIndex = Math.Min(this.listing?.RootOptions.Count ?? 0, newListing.RootOptions.Count);
			for (int i = 0; i < maxOptionIndex; i++)
				configureNodeOpenings(this.listing.RootOptions[i], newListing.RootOptions[i]);

			this.displayingFor = shelf;
			this.listing = newListing;

            RebuildReservationOptions();    //Perform after nodeOpening configuration as reservations will change
		}

        public void RebuildReservationOptions()
        {
            if(listing.RootOptions.Count > ReservationOptionIndex)
                listing.RootOptions.RemoveAt(ReservationOptionIndex);

            var reservations = displayingFor.AllReservations;

            if(!reservations.Any())
                return;
        
            TreeNode_UIOption reservationOptions= new TreeNode_UIOption("AS.ReservationsRootNode.Label".Translate());              
            foreach(var reservation in reservations)
                (new TreeNode_UIOption_ReservedItem(displayingFor, reservation.Value, reservation.Key))
                    .AddAsChildTo(reservationOptions);
                    
            listing.RootOptions.Insert(ReservationOptionIndex, reservationOptions); 
        }

        //If someone selects another Shelf while Tab is open, OnOpen won't be called inbetween
		protected override void FillTab() {
            Building_Shelf shelf = this.SelObject as Building_Shelf;
            if (shelf == null)
                return;
            if(shelf != this.displayingFor)
                CreateListingFor(shelf);
        
			Rect rect = new Rect (0, 30, ITab_Stock.WinSize.x, ITab_Stock.WinSize.y - 30).ContractedBy(CellSpacing);
			listing.Begin (rect);
			listing.DrawUIOptions ();
			listing.End ();
		}

        public void CreateListingFor(Building_Shelf shelf)
        {
            SetupListing(shelf);
            if(!shelf.filterChangedSignalManager.receivers.Contains(this)) {
                shelf.filterChangedSignalManager.RegisterReceiver(this);
                shelf.itemsHeldChangedSignalManager.RegisterReceiver(this);
            }
        }

		public void Notify_SignalReceived(Signal signal)
		{
			Building_Shelf shelf = signal.args[0] as Building_Shelf;
			if (shelf != displayingFor) { //Cannot remove OnClose, will do so when erroneous signal sent
				shelf.filterChangedSignalManager.DeregisterReceiver(this);
				shelf.itemsHeldChangedSignalManager.DeregisterReceiver(this);
			}
			else
				SetupListing(shelf);
		}

        public override void OnOpen()
        {
            Building_Shelf shelf = this.SelObject as Building_Shelf;
            if (shelf == null)
                return;
            if(shelf != this.displayingFor) 
                CreateListingFor(shelf);
            else
                RebuildReservationOptions();
        }
    }
}

