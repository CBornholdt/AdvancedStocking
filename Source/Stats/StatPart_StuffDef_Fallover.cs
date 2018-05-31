using System;
using System.Xml;
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

    //Copied mostly from ThingCountClass
    public class ThingValueClass 
	{
    	public ThingDef thingDef;

        public float value;

        public string Summary {
            get {
                return this.value + " Value For " + ((this.thingDef == null) ? "null" : this.thingDef.label);
            }
        }

        public ThingValueClass()
        {
        }

        public ThingValueClass(ThingDef thingDef, float value)
        {
            this.thingDef = thingDef;
            this.value = value;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1) {
                Log.Error("Misconfigured ThingValue: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
            this.value = (float)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                    "(",
                    this.value,
                    " Value For ",
                    (this.thingDef == null) ? "null" : this.thingDef.defName,
                    ")"
            });
        }

    /*    public override int GetHashCode()
        {
			return (int)this.thingDef.shortHash + ((int)this.value) << 16;
        }*/
    }

	public class ThingCategoryValueClass
    {
        public ThingCategoryDef thingCatDef;

        public float value;

        public string Summary {
            get {
                return this.value + " Value For " + ((this.thingCatDef == null) ? "null" : this.thingCatDef.label);
            }
        }

        public ThingCategoryValueClass()
        {
        }

        public ThingCategoryValueClass(ThingCategoryDef thingCatDef, float value)
        {
            this.thingCatDef = thingCatDef;
            this.value = value;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1) {
                Log.Error("Misconfigured ThingValue: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingCatDef", xmlRoot.Name);
            this.value = (float)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                    "(",
                    this.value,
                    " Value For ",
                    (this.thingCatDef == null) ? "null" : this.thingCatDef.defName,
                    ")"
            });
        }

    /*    public override int GetHashCode()
        {
            return (int)this.thingCatDef.shortHash + ((int)this.value) << 16;
        }*/
    }
    
	public class StuffCategoryValueClass
    {
        public StuffCategoryDef stuffCatDef;

        public float value;

        public string Summary {
            get {
                return this.value + " Value For " + ((this.stuffCatDef == null) ? "null" : this.stuffCatDef.label);
            }
        }
        
        public StuffCategoryValueClass()
        {
        }
        
        public StuffCategoryValueClass(StuffCategoryDef stuffCatDef, float value)
        {
            this.stuffCatDef = stuffCatDef;
            this.value = value;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1) {
                Log.Error("Misconfigured ThingValue: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stuffCatDef", xmlRoot.Name);
            this.value = (float)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                    "(",
                    this.value,
                    " Value For ",
                    (this.stuffCatDef == null) ? "null" : this.stuffCatDef.defName,
                    ")"
            });
        }

    /*    public override int GetHashCode()
        {
            return (int)this.stuffCatDef.shortHash + ((int)this.value) << 16;
        }*/
    }
}

