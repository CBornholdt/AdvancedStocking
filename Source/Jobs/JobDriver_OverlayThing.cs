using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public class JobDriver_OverlayThing : JobDriver
	{
		private float totalWorkNeeded;
		private float workPerformed;

		protected Building_Shelf Shelf {
			get {
				return TargetThingA as Building_Shelf;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFail)
		{
			return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null) && this.pawn.Reserve(this.job.targetB, this.job, 1, -1, null) &&
				this.pawn.Reserve(this.job.targetC, this.job, 1, -1, null);
		}

		public override void ExposeData ()
		{
			base.ExposeData ();
			Scribe_Values.Look<float> (ref this.totalWorkNeeded, "totalWorkNeeded");
			Scribe_Values.Look<float> (ref this.workPerformed, "workPerformed");
		}
			
		protected override IEnumerable<Toil> MakeNewToils (){
			this.FailOnDespawnedNullOrForbidden (TargetIndex.A);
			this.FailOnDestroyedOrNull (TargetIndex.B);
			yield return Toils_Goto.GotoThing (TargetIndex.B, PathEndMode.Touch);
            
			Toil doWork = new Toil ();
			doWork.initAction = delegate {
				this.totalWorkNeeded = Shelf.OverlayWorkNeeded (TargetC.Cell);
				this.workPerformed = 0;
			};
			doWork.tickAction = delegate {
				this.workPerformed += this.pawn.GetStatValue (StatDefOf.WorkSpeedGlobal, true);
				if (this.workPerformed >= this.totalWorkNeeded)
					doWork.actor.jobs.curDriver.ReadyForNextToil ();
			};
			doWork.defaultCompleteMode = ToilCompleteMode.Never;
			doWork.WithProgressBar (TargetIndex.A, () => this.workPerformed / this.totalWorkNeeded, false, -0.5f);
			yield return doWork;

			yield return new Toil {
				initAction = delegate {
					Shelf.OverlayThing (TargetThingB, TargetC.Cell);
					this.pawn.Map.reservationManager.Release (this.job.targetA, this.pawn, this.job);
					this.pawn.Map.reservationManager.Release (this.job.targetB, this.pawn, this.job);
					this.pawn.Map.reservationManager.Release (this.job.targetC, this.pawn, this.job);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}

