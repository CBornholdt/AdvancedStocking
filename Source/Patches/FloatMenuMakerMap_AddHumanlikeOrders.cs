using Verse;
using Verse.AI;
using RimWorld;
using Harmony;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace AdvancedStocking
{
	public class FloatMenuMakerMap_AddHumanlikeOrders
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			IntVec3 c = IntVec3.FromVector3(clickPos);
			if (pawn.equipment != null) {
				//First equipment already handled by patched Method, so skip
				var equipmentList = c.GetThingList(pawn.Map).Where(t => t.TryGetComp<CompEquippable>() != null).OfType<ThingWithComps>().Skip(1);

				//Log.Message(string.Format("Adding {0:d} equipment orders", equipmentList.Count()));

				foreach (ThingWithComps equipment in equipmentList) {
					string labelShort = equipment.LabelShort;
					FloatMenuOption item3;
					if (equipment.def.IsWeapon && pawn.story.WorkTagIsDisabled(WorkTags.Violent)) {
						item3 = new FloatMenuOption("CannotEquip".Translate(new object[] {
							labelShort
						}) + " (" + "IsIncapableOfViolenceLower".Translate(new object[] {
							pawn.LabelShort
						}) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn)) {
						item3 = new FloatMenuOption("CannotEquip".Translate(new object[] {
							labelShort
						}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
						item3 = new FloatMenuOption("CannotEquip".Translate(new object[] {
							labelShort
						}) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else {
						string text3 = "Equip".Translate(new object[] {
							labelShort
						});
						if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler)) {
							text3 = text3 + " " + "EquipWarningBrawler".Translate();
						}
						item3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, delegate {
							equipment.SetForbidden(false, true);
							pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Equip, equipment), JobTag.Misc);
							MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
							PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
						}, MenuOptionPriority.High, null, null, 0f, null, null), pawn, equipment, "ReservedBy");
					}
					opts.Add(item3);
				}
			}
			if (pawn.apparel != null) {
				//First apparel already handled by patched method, so skip
				var apparelList = c.GetThingList(pawn.Map).OfType<Apparel>().Skip(1);

				//Log.Message(string.Format("Adding {0:d} apparel orders", apparelList.Count()));

				foreach (Apparel apparel in apparelList) {
					FloatMenuOption item4;
					if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn)) {
						item4 = new FloatMenuOption("CannotWear".Translate(new object[] {
							apparel.Label
						}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def)) {
						item4 = new FloatMenuOption("CannotWear".Translate(new object[] {
							apparel.Label
						}) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else {
						item4 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ForceWear".Translate(new object[] {
							apparel.LabelShort
						}), delegate {
							apparel.SetForbidden(false, true);
							Job job = new Job(JobDefOf.Wear, apparel);
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						}, MenuOptionPriority.High, null, null, 0f, null, null), pawn, apparel, "ReservedBy");
					}
					opts.Add(item4);
				}
			}
            AddReservationActionsIfOnlySingleItem(c, pawn.MapHeld, opts);
		}

        static public void AddReservationActionsIfOnlySingleItem(IntVec3 cell, Map map, List<FloatMenuOption> opts)
        {
            var shelf = cell.GetShelf(map);
            if(shelf == null)
                return;
            var itemsAtCell = cell.GetThingList(map).Where(thing => thing.def.category == ThingCategory.Item).ToList();
            if(itemsAtCell.Count == 1) {
                var item = itemsAtCell[0];
                ThingDef reservedItem = shelf.GetReservationAt(cell);
                if(reservedItem == null)
                    opts.Add(new FloatMenuOption("AS.AddReservation.FloatOption".Translate(item.def.LabelCap)
                                                    , () => shelf.AddReservation(item)));
                else if(reservedItem == item.def)
                    opts.Add(new FloatMenuOption("AS.RemoveReservation.FloatOption".Translate(item.def.LabelCap)
                                                    , () => shelf.RemoveReservation(item)));
                else
                    opts.Add(new FloatMenuOption("AS.AddReservation.AlreadyReserved.FloatOption".Translate(item.def.LabelCap, reservedItem.LabelCap), null));
            } 
        }
	}
}
