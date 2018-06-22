using System;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace AdvancedStocking
{
	public class StatWorker_Stocking_OverstackRatio : StatWorker_Stocking
	{
		public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
			return (float)(req.Thing as Building_Shelf)?.OverstackLimitPerOverlay;
		}
        
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Building_Shelf shelf = req.Thing as Building_Shelf;

            stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.ModSettings".Translate(AS_Mod.settings.maxOverstackRatio));

            if (AS_Mod.settings.overlaysReduceStacklimit) {
                if (AS_Mod.settings.overlaysReduceStacklimitPartially)
                    stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.OverlayReduction.Partial"
                            .Translate(shelf.OverlayLimit * 2f / 3f + 1f / 3f));
                else
                    stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.OverlayReduction".Translate(shelf.OverlayLimit));
            }
            return stringBuilder.ToString();                                                                                                                               
        }
	}
}
