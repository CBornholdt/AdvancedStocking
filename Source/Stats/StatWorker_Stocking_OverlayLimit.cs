using System;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace AdvancedStocking
{
	public class StatWorker_Stocking_OverlayLimit : StatWorker_Stocking
	{
        public override bool IsDisabledFor(Thing thing)
        {
            return base.IsDisabledFor(thing) && !((thing as Building_Shelf)?.InRackMode ?? false);
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
            return (float)(req.Thing as Building_Shelf)?.MaxOverlayLimit;
		}

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Building_Shelf shelf = req.Thing as Building_Shelf;

            stringBuilder.AppendLine("StatWorker.OverlayLimit.Desc.ModSettings".Translate(AS_Mod.settings.maxOverlayLimit));
            if (shelf.MaxOverlayLimit < AS_Mod.settings.maxOverlayLimit && shelf.HeaviestAllowedThing != null)
                stringBuilder.AppendLine("StatWorker.OverlayLimit.Desc.MassContraint"
                    .Translate(shelf.HeaviestAllowedThing.LabelCap
                            , shelf.HeaviestAllowedThing.GetStatValueAbstract(StatDefOf.Mass)
                            , shelf.GetStatValue(StockingStatDefOf.MaxStockWeight)));
            
            return stringBuilder.ToString();                                                                                                                               
        }
	}
}
