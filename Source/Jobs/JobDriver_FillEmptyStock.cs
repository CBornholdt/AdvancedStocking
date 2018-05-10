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
		}
	}
}

