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
			Building_Shelf shelf = req.Thing as Building_Shelf;
			return Mathf.Min(shelf.CurrentOrganizeMode.overstackRatioLimit, AS_Mod.settings.maxOverstackRatio);
		}
        
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Building_Shelf shelf = req.Thing as Building_Shelf;

            stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.MinValue".Translate());
            stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.ModSettings".Translate(AS_Mod.settings.maxOverstackRatio));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("StatWorker.OverstackLimit.Desc.OrgModeSettings".Translate(shelf.CurrentOrganizeMode.label
                                                                                        , shelf.CurrentOrganizeMode.overstackRatioLimit));
            return stringBuilder.ToString();                                                                                                                               
        }
	}
}
