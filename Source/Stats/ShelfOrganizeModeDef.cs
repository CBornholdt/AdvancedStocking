using System;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace AdvancedStocking
{
	public class ShelfOrganizeModeDef : Def
	{
		public bool allowOverstackMode = false;
		public bool allowOverlayMode = false;

		public int order;
		public int overlayLimit = 1;
		public int overstackRatioLimit = 1;
		public int numAllowedThingDefs = -1;
		public List<ThingCategoryDef> allowedThingCategories;
		public List<ThingDef> allowedThingDefs;
		public List<ThingCategoryDef> disallowedThingCategories;
		public List<ThingDef> disallowedThingDefs;


		public override void ResolveReferences()
		{
			base.ResolveReferences ();
			for (int i = 0; i < (this.allowedThingDefs?.Count ?? 0); i++)
				this.allowedThingDefs [i].ResolveReferences ();
			for (int i = 0; i < (this.allowedThingCategories?.Count ?? 0); i++)
				this.allowedThingCategories [i].ResolveReferences ();
			for (int i = 0; i < (this.disallowedThingDefs?.Count ?? 0); i++)
				this.disallowedThingDefs [i].ResolveReferences ();
			for (int i = 0; i < (this.disallowedThingCategories?.Count ?? 0); i++)
				this.disallowedThingCategories [i].ResolveReferences ();
		}
	}
}

