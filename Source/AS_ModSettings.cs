using System;
using System.Linq;
using UnityEngine;
using Verse;
namespace AdvancedStocking
{
    public class AS_ModSettings : ModSettings
    {
        public float maxOverstackRatio = 10f;
        public float maxOverlayLimit = 10f;
        public bool overlaysReduceStacklimit = true;
        public bool overlaysReduceStacklimitPartially = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.maxOverstackRatio, "MaxOverstackRatio", 10f);
            Scribe_Values.Look<float>(ref this.maxOverlayLimit, "MaxOverlayLimit", 10f);
            Scribe_Values.Look<bool>(ref this.overlaysReduceStacklimit, "OverlaysReduceStackLimit", true);
            Scribe_Values.Look<bool>(ref this.overlaysReduceStacklimitPartially, "OverlaysReduceStackLimitPartially", false);
        }
    }

    public class AS_Mod : Mod
    {
        static public AS_ModSettings settings;

        public AS_Mod(ModContentPack contentPack) : base(contentPack)
        {
            settings = GetSettings<AS_ModSettings>();
        }

        public override string SettingsCategory() => "AS_Mod.CategoryLabel".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            Rect paneRect = inRect.ContractedBy(100);
            listing.Begin(paneRect);
            
            listing.Label("AS_Mod.MaxOverstackRatio.Label".Translate(settings.maxOverstackRatio.ToString()));
            settings.maxOverstackRatio = listing.Slider(settings.maxOverstackRatio, 0.25f, 20f, 0.1f);
            Rect overstackRect = new Rect(0, 0, paneRect.width, listing.CurHeight);
            Widgets.DrawHighlightIfMouseover(overstackRect);
            TooltipHandler.TipRegion(overstackRect, new TipSignal("AS_Mod.MaxOverstackRatio.Tooltip".Translate()));

            listing.Label("AS_Mod.MaxOverlayLimit.Label".Translate(settings.maxOverlayLimit.ToString()));
            settings.maxOverlayLimit = listing.Slider(settings.maxOverlayLimit, 1f, 20f, 1f);
            Rect overlayRect = new Rect(0, overstackRect.yMax, paneRect.width, listing.CurHeight - overstackRect.height);
            Widgets.DrawHighlightIfMouseover(overlayRect);
            TooltipHandler.TipRegion(overlayRect, new TipSignal("AS_Mod.MaxOverlayLimit.Tooltip".Translate()));
            
            listing.ColumnWidth = listing.ColumnWidth / 2;
            listing.CheckboxLabeled("AS_MOD.OverlaysReduceStacklimit.Label".Translate(), ref settings.overlaysReduceStacklimit
                                    , "AS_MOD.OverlaysReduceStacklimit.Tooltip".Translate());
            if (settings.overlaysReduceStacklimit)
                listing.CheckboxLabeled("AS_MOD.OverlaysReduceStacklimit.Partially.Label".Translate()
                    , ref settings.overlaysReduceStacklimitPartially, "AS_MOD.OverlaysReduceStacklimit.Partially.Tooltip".Translate());
            listing.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            if(Current.Game != null && Current.ProgramState == ProgramState.Playing)
                foreach (var shelf in Find.Maps.SelectMany(map => map.listerBuildings.AllBuildingsColonistOfClass<Building_Shelf>()))
                    shelf.RecalcOverlays();
        }
    }
}
