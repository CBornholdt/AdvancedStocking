using System;
using Verse.AI;
using System.Collections.Generic;

namespace AdvancedStocking
{
	public class JobDriver_FillEmptyStock : JobDriver_HaulToCell
	{
		protected override IEnumerable<Toil> MakeNewToils()
		{
			foreach (Toil toil in base.MakeNewToils())
				yield return toil;

			Building_Shelf shelf = TargetC.Thing as Building_Shelf;
			if (shelf != null)
				yield return new Toil () {
					initAction = delegate { 
						if(shelf.IsOrganizingEnabled)
							shelf.TrySetupAutoOrganizeJob (this.pawn);
					},
					defaultCompleteMode = ToilCompleteMode.Instant
				};
		}
	}
}

