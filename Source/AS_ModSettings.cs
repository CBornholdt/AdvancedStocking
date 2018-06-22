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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.maxOverstackRatio, "MaxOverstackRatio");
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
            listing.Begin(inRect.ContractedBy(100));
            
            listing.Label("AS_Mod.MaxOverstackRatio.Label".Translate(settings.maxOverstackRatio.ToString()));
            settings.maxOverstackRatio = listing.Slider(settings.maxOverstackRatio, 0.25f, 20f, 0.1f);
            listing.Label("AS_Mod.MaxOverlayLimit.Label".Translate(settings.maxOverlayLimit.ToString()));
            settings.maxOverlayLimit = listing.Slider(settings.maxOverlayLimit, 1f, 20f, 1f);
            listing.ColumnWidth = listing.ColumnWidth / 2;
            listing.CheckboxLabeled("AS_MOD.OverlaysReduceStacklimit.Label".Translate(), ref settings.overlaysReduceStacklimit
                                    , "AS_MOD.OverlaysReduceStacklimit.Tooltip".Translate());
            listing.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            foreach (var shelf in Find.Maps.SelectMany(map => map.listerBuildings.AllBuildingsColonistOfClass<Building_Shelf>()))
                shelf.RecalcOverlays();
        }
    }
}
