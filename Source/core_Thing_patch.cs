using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;
using UnityEngine;

// Analysis disable once CheckNamespace
namespace AdvancedStocking
{
	[StaticConstructorOnStartup]
	static class HarmonyPatches
	{
		static HarmonyPatches() {
			HarmonyInstance harmony = HarmonyInstance.Create ("rimworld.advancedstocking");

			harmony.Patch (AccessTools.Method (typeof(Verse.Thing), "TryAbsorbStack"), null, 
				new HarmonyMethod (typeof(AdvancedStocking.HarmonyPatches).GetMethod("TryAbsorbStack_Postfix")), null);

			harmony.Patch(AccessTools.Method(typeof(Verse.Thing), "SpawnSetup"), null, null, 
				new HarmonyMethod(typeof(AdvancedStocking.HarmonyPatches).GetMethod("SpawnSetup_Transpiler")));

			harmony.Patch (AccessTools.Method (typeof(RimWorld.StorageSettings), "set_Priority"), 
				new HarmonyMethod (typeof(AdvancedStocking.HarmonyPatches).GetMethod("set_Priority_Prefix")), null, null);

			harmony.Patch(AccessTools.Method(typeof(RimWorld.FloatMenuMakerMap), "AddHumanlikeOrders"), null, 
				new HarmonyMethod (typeof(AdvancedStocking.HarmonyPatches).GetMethod("AddHumanlikeOrders_Postfix")), null);

			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettingsClipboard), "CopyPasteGizmosFor"), null, 
				new HarmonyMethod (typeof(AdvancedStocking.StockingSettingsClipboard).GetMethod("CopyPasteGizmosFor_Postfix")), null);

			harmony.Patch(AccessTools.Method(typeof(Verse.TerrainGrid), "DoTerrainChangedEffects"), null,
				new HarmonyMethod(typeof(AdvancedStocking.HarmonyPatches).GetMethod("DoTerrainChangedEffects_Postfix")), null);
		}

		public static void DoTerrainChangedEffects_Postfix(TerrainGrid __instance, IntVec3 c)
		{
			Map map = (Map)typeof(TerrainGrid).
				GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

			(map.slotGroupManager.SlotGroupAt(c)?.parent as Building_Shelf)?.CacheStats();
		}

		public static void TryAbsorbStack_Postfix(Thing __instance, Thing other, bool respectStackLimit, ref bool __result) {
			if (__instance == null || __instance.def.category != ThingCategory.Item || !__instance.Spawned)
				return;
			if (__instance.stackCount >= __instance.def.stackLimit) {
				SlotGroup slotGroup = __instance.Position.GetSlotGroup (__instance.MapHeld);
				if (slotGroup != null && slotGroup.parent != null) {
					slotGroup.parent.Notify_ReceivedThing (__instance);
				}
			}
		}

		public static IEnumerable<CodeInstruction> SpawnSetup_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = instructions.ToList ();
			FieldInfo thingDefStacklimit = AccessTools.Field (typeof(ThingDef), "stackLimit");
			MethodInfo spawnSetupHelper = AccessTools.Method (typeof(HarmonyPatches), "SpawnSetupHelper_IgnoreOverstack");

			for(int i = 0; i < codes.Count; i++) {

				if((i + 1) < codes.Count && codes[i].opcode == OpCodes.Ldfld && codes[i].operand == thingDefStacklimit 
					&& codes[i + 1].opcode == OpCodes.Ble) {
					yield return codes [i];
					yield return codes [i + 1];
					yield return new CodeInstruction (OpCodes.Ldarg_0);	//Leave Thing on stack
					yield return new CodeInstruction (OpCodes.Ldarg_2); //Leave Thing, Bool on stack
					yield return new CodeInstruction (OpCodes.Call, spawnSetupHelper);	//Consume 2, leave bool
					yield return new CodeInstruction (OpCodes.Brtrue, codes[i+1].operand); //Consume bool
					i++;
				}
				else
					yield return codes [i];
			}
		}

		public static bool SpawnSetupHelper_IgnoreOverstack(Thing thing, bool respawningAfterLoad)
		{
			if (respawningAfterLoad)
				Current.Game.GetComponent<StockingGameComponent> ().AddThingForOverstackCheck (thing);
			return respawningAfterLoad;
		}

		public static void set_Priority_Prefix(StorageSettings __instance, ref StoragePriority value)
		{
			Building_Shelf shelf = __instance.owner as Building_Shelf;
			if(value != __instance.Priority)
				shelf?.Notify_PriorityChanging(value);
		}

		public static void AddHumanlikeOrders_Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			IntVec3 c = IntVec3.FromVector3 (clickPos);
			if (pawn.equipment != null) {
				//First equipment already handled by patched Method, so skip
				var equipmentList = c.GetThingList (pawn.Map).Where (t => t.TryGetComp<CompEquippable> () != null).OfType<ThingWithComps> ().Skip (1);

				//Log.Message(string.Format("Adding {0:d} equipment orders", equipmentList.Count()));

				foreach (ThingWithComps equipment in equipmentList) {
					string labelShort = equipment.LabelShort;
					FloatMenuOption item3;
					if (equipment.def.IsWeapon && pawn.story.WorkTagIsDisabled (WorkTags.Violent)) {
						item3 = new FloatMenuOption ("CannotEquip".Translate (new object[] {
							labelShort
						}) + " (" + "IsIncapableOfViolenceLower".Translate (new object[] {
							pawn.LabelShort
						}) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					} else if (!pawn.CanReach (equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn)) {
						item3 = new FloatMenuOption ("CannotEquip".Translate (new object[] {
							labelShort
						}) + " (" + "NoPath".Translate () + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					} else if (!pawn.health.capacities.CapableOf (PawnCapacityDefOf.Manipulation)) {
						item3 = new FloatMenuOption ("CannotEquip".Translate (new object[] {
							labelShort
						}) + " (" + "Incapable".Translate () + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					} else {
						string text3 = "Equip".Translate (new object[] {
							labelShort
						});
						if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait (TraitDefOf.Brawler)) {
							text3 = text3 + " " + "EquipWarningBrawler".Translate ();
						}
						item3 = FloatMenuUtility.DecoratePrioritizedTask (new FloatMenuOption (text3, delegate {
							equipment.SetForbidden (false, true);
							pawn.jobs.TryTakeOrderedJob (new Job (JobDefOf.Equip, equipment), JobTag.Misc);
							MoteMaker.MakeStaticMote (equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
							PlayerKnowledgeDatabase.KnowledgeDemonstrated (ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
						}, MenuOptionPriority.High, null, null, 0f, null, null), pawn, equipment, "ReservedBy");
					}
					opts.Add (item3);
				}
			}
			if (pawn.apparel != null) {
				//First apparel already handled by patched method, so skip
				var apparelList = c.GetThingList (pawn.Map).OfType<Apparel> ().Skip(1);

				//Log.Message(string.Format("Adding {0:d} apparel orders", apparelList.Count()));

				foreach(Apparel apparel in apparelList) {
					FloatMenuOption item4;
					if (!pawn.CanReach (apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn)) {
						item4 = new FloatMenuOption ("CannotWear".Translate (new object[] {
							apparel.Label
						}) + " (" + "NoPath".Translate () + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					} else if (!ApparelUtility.HasPartsToWear (pawn, apparel.def)) {
						item4 = new FloatMenuOption ("CannotWear".Translate (new object[] {
							apparel.Label
						}) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate () + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					} else {
						item4 = FloatMenuUtility.DecoratePrioritizedTask (new FloatMenuOption ("ForceWear".Translate (new object[] {
							apparel.LabelShort
						}), delegate {
							apparel.SetForbidden (false, true);
							Job job = new Job (JobDefOf.Wear, apparel);
							pawn.jobs.TryTakeOrderedJob (job, JobTag.Misc);
						}, MenuOptionPriority.High, null, null, 0f, null, null), pawn, apparel, "ReservedBy");
					}
					opts.Add (item4);
				}
			}
		}
	}
}
