﻿using System;
using System.Collections;
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
		private Building_Shelf displayingFor;

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

		private void SetupListing(Building_Shelf s)
		{
			TreeNode_UIOptionCheckbox stockingEnabledCheckbox = new TreeNode_UIOptionCheckbox ("InStockingMode_label".Translate(), 
				() => s.InStockingMode, b => s.InStockingMode = b, "InStockingMode_tooltip".Translate());

			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("InPriorityCyclingMode_label".Translate(), 
				() => s.InPriorityCyclingMode, b => s.InPriorityCyclingMode = b, "InPriorityCyclingMode_tooltip".Translate(), false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("InSingleThingMode_label".Translate(), 
				() => s.InSingleThingMode, b => s.InSingleThingMode = b, "Testing", false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("InForbiddenMode_label".Translate(), 
				() => s.InForbiddenMode, b => s.InForbiddenMode = b, "InForbiddenMode_tooltip".Translate(), false, () => s.InStockingMode));

			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("InOverStockMode_label".Translate(), 
				() => s.InOverStockMode, b => s.InOverStockMode = b, "InOverStockMode_tooltip".Translate(), false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingOrganizeMode>("StockingOrganizeMode_label".Translate(),
				() => Enum.GetName(typeof(StockingOrganizeMode), s.OrganizeMode).Translate(), p => s.OrganizeMode = p, null, 
				ITab_Stock.PriorityButtonWidth, "StockingOrganizeMode_tooltip".Translate(), false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add (new TreeNode_UIOptionCheckbox ("AutoOrganizeAfterFilling_label".Translate (),
				() => s.PawnShouldOrganizeAfterFilling, b => s.PawnShouldOrganizeAfterFilling = b, "AutoOrganizeAfterFilling_tooltip".Translate (), 
				false, () => s.InStockingMode && s.IsOrganizingEnabled));

			stockingEnabledCheckbox.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Fill_Empty_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), s.FillEmptyStockPriority).Translate(), 
				p => s.FillEmptyStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Fill_Empty_Stock_Priority_Tooltip".Translate(), false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Organize_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), s.OrganizeStockPriority).Translate(), 
				p => s.OrganizeStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Organize_Stock_Priority_Tooltip".Translate(), false, () => s.InStockingMode));
			stockingEnabledCheckbox.children.Add( new TreeNode_UIOption_EnumMenuButton<StockingPriority>("Push_Full_Stock_Priority".Translate(), 
				() => Enum.GetName(typeof(StockingPriority), s.PushFullStockPriority).Translate(), 
				p => s.PushFullStockPriority = p, 
				null, ITab_Stock.PriorityButtonWidth, "Push_Full_Stock_Priority_Tooltip".Translate(), false, () => s.InStockingMode));

			this.listing = new Listing_TreeUIOption (stockingEnabledCheckbox);
		}

		protected override void FillTab() {
			Building_Shelf s = this.SelObject as Building_Shelf;
			if (s == null)
				return;
			if (s != this.displayingFor) {
				SetupListing (s);
				this.displayingFor = s;
			}
			Rect rect = new Rect (0, 30, ITab_Stock.WinSize.x, ITab_Stock.WinSize.y);
			listing.Begin (rect);
			listing.DrawUIOptions ();
			listing.End ();
		}
	}
}

