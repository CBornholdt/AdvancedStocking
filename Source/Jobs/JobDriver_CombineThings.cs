using System;
using System.Diagnostics;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace AdvancedStocking
{
	public class JobDriver_CombineThings : JobDriver {
        
		private float totalWorkNeeded;
		private float workPerformed;

		protected Building_Shelf Shelf {
			get {
				return TargetThingA as Building_Shelf;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed){
			return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null) && this.pawn.Reserve(this.job.targetB, this.job, 1, -1, null) &&
					   this.pawn.Reserve(this.job.targetC, this.job, 1, -1, null);
		}

		public override void ExposeData ()
		{
			base.ExposeData ();
			Scribe_Values.Look<float> (ref this.totalWorkNeeded, "totalWorkNeeded");
			Scribe_Values.Look<float> (ref this.workPerformed, "workPerformed");
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils (){
			this.FailOnDespawnedNullOrForbidden (TargetIndex.A);
			this.FailOnDestroyedOrNull (TargetIndex.B);
			this.FailOnDespawnedOrNull (TargetIndex.C);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);

			Toil doWork = new Toil ();
			doWork.initAction = delegate {
				this.totalWorkNeeded = Shelf.CombineWorkNeeded (TargetC.Thing);
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
					Shelf.OverstackThings (TargetThingB, TargetC.Thing);
					this.pawn.Map.reservationManager.Release (this.job.targetA, this.pawn, this.job);
					this.pawn.Map.reservationManager.Release (this.job.targetC, this.pawn, this.job);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
    }
}