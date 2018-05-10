﻿using System;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	public class StatWorker_Stocking_OverlayLimit : StatWorker_Stocking
	{
		public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
			Building_Shelf shelf = req.Thing as Building_Shelf;
			return (float)shelf.CurrentOrganizeMode.overlayLimit;
		}
	}
}