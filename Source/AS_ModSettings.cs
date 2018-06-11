using System;
using UnityEngine;
using Verse;
namespace AdvancedStocking
{
    public class AS_ModSettings : ModSettings
    {
        public float maxOverstackRatio = 10f;

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
            settings.maxOverstackRatio = listing.Slider(settings.maxOverstackRatio, 0.5f, 20f);
            listing.End();
        }
    }
}
