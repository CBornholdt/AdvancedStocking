using System;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	[DefOf]
	public static class StockingJobDefOf {
		public static JobDef CombineThings;
		public static JobDef OverlayThing;
		public static JobDef FillEmptyStock;
		public static JobDef PushFullStock;
	}

	[DefOf]
	public static class StockingStatCategoryDefOf {
		public static StatCategoryDef Stocking;
	}

	[DefOf]
	public static class StockingStatDefOf {
		public static StatDef MaxStockWeight;
		public static StatDef MaxOverlayLimit;
		public static StatDef MaxOverstackRatio;
	}

	[DefOf]
	public static class ShelfOrganizeModeDefOf {
		public static ShelfOrganizeModeDef None;
		public static ShelfOrganizeModeDef Overstack;
		public static ShelfOrganizeModeDef SingleThingOverlay;
		public static ShelfOrganizeModeDef ApparelOverlay;
		public static ShelfOrganizeModeDef WeaponsOverlay;
	}

	[DefOf]
	public static class StockWorkGiverDefs 
	{
		public static WorkGiverDef OrganizeStock_High;
		public static WorkGiverDef OrganizeStock_Normal;
		public static WorkGiverDef OrganizeStock_Low;

		public static WorkGiverDef FillEmptyStock_High;
		public static WorkGiverDef FillEmptyStock_Normal;
		public static WorkGiverDef FillEmptyStock_Low;

		public static WorkGiverDef PushFullStock_High;
		public static WorkGiverDef PushFullStock_Normal;
		public static WorkGiverDef PushFullStock_Low;
	}
}
