using System;
using System.Xml;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse;

namespace AdvancedStocking
{
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
}
