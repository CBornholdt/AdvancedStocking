using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse;

namespace AdvancedStocking
{
	public class StatPart_StuffDef_Fallover : StatPart
    {
		List<ThingValueClass> thingFactors = null;
        List<ThingCategoryValueClass> thingCategoryFactors = null;
		List<StuffCategoryValueClass> stuffCategoryFactors = null;

		public override void TransformValue(StatRequest req, ref float val)
		{
			//If existing StatOffset or StatFactor then stat has already been adjusted
			if (req.StuffDef == null)
				return;

			var stuffProps = req.StuffDef.stuffProps;
			if ((stuffProps.statOffsets?.Any(mod => mod.stat == this.parentStat) ?? false)
			    || (stuffProps.statFactors?.Any(mod => mod.stat == this.parentStat) ?? false))
				return;

			var thingFactor = thingFactors?.FirstOrDefault(tf => tf.thingDef == req.StuffDef);
			if (thingFactor != null) {
				val *= thingFactor.value;
				return;
			}

			var thingCatFactor = thingCategoryFactors?.FirstOrDefault(tcf => tcf.thingCatDef.DescendantThingDefs.Contains(req.StuffDef));
			if (thingCatFactor != null) {
				val *= thingCatFactor.value;
				return;
			}

			var stuffCatFactor = stuffCategoryFactors?.FirstOrDefault(scf => stuffProps.categories.Contains(scf.stuffCatDef));
			if (stuffCatFactor != null) {
				val *= stuffCatFactor.value;
				return;
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (req.StuffDef == null)
				return null;
           
			var stuffProps = req.StuffDef.stuffProps;
            if ((stuffProps.statOffsets?.Any(mod => mod.stat == this.parentStat) ?? false)
                || (stuffProps.statFactors?.Any(mod => mod.stat == this.parentStat) ?? false))
                return null;

            var thingFactor = thingFactors?.FirstOrDefault(tf => tf.thingDef == req.StuffDef);
            if (thingFactor != null) {
				return "StatReport_StuffDef_Fallover.ThingDef".Translate(thingFactor.thingDef.LabelCap) + ": x" + thingFactor.value.ToStringPercent();
            }

            var thingCatFactor = thingCategoryFactors?.FirstOrDefault(tcf => tcf.thingCatDef.DescendantThingDefs.Contains(req.StuffDef));
            if (thingCatFactor != null) {
				return "StatReport_StuffDef_Fallover.ThingCatDef".Translate(thingCatFactor.thingCatDef.LabelCap) + ": x" + thingCatFactor.value.ToStringPercent();
            }

            var stuffCatFactor = stuffCategoryFactors?.FirstOrDefault(scf => stuffProps.categories.Contains(scf.stuffCatDef));
            if (stuffCatFactor != null) {
				return "StatReport_StuffDef_Fallover.StuffCatDef".Translate(stuffCatFactor.stuffCatDef.LabelCap) + ": x" + stuffCatFactor.value.ToStringPercent();
            }

			return null;
		}
	}
}
