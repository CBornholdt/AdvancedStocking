using System;
using System.Xml;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse;

namespace AdvancedStocking
{
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
	}
}
