using System;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace AdvancedStocking
{
	public class StatWorker_Stocking_OverlayLimit : StatWorker_Stocking
	{
		public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
			Building_Shelf shelf = req.Thing as Building_Shelf;
            return Mathf.Min((float)shelf.CurrentOrganizeMode.overlayLimit, AS_Mod.settings.maxOverlayLimit);
		}

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Building_Shelf shelf = req.Thing as Building_Shelf;

            stringBuilder.AppendLine("StatWorker.OverlayLimit.Desc.MinValue".Translate());
            stringBuilder.AppendLine("StatWorker.OverlayLimit.Desc.ModSettings".Translate(AS_Mod.settings.maxOverlayLimit));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("StatWorker.OverlayLimit.Desc.OrgModeSettings".Translate(shelf.CurrentOrganizeMode.label
                                                                                        , shelf.CurrentOrganizeMode.overlayLimit));
            return stringBuilder.ToString();                                                                                                                               
        }
	}
}
