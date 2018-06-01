using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Harmony;

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

			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettings), "set_Priority"),
				new HarmonyMethod(typeof(AdvancedStocking.HarmonyPatches).GetMethod("set_Priority_Prefix")), null, null);

			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettingsClipboard), "CopyPasteGizmosFor"), null, 
				new HarmonyMethod (typeof(AdvancedStocking.StockingSettingsClipboard).GetMethod("CopyPasteGizmosFor_Postfix")), null);

			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static void TryAbsorbStack_Postfix(Thing __instance, Thing other, bool respectStackLimit, ref bool __result) {
			if (__instance == null || __instance.def.category != ThingCategory.Item || !__instance.Spawned)
				return;
			if (__instance.stackCount >= __instance.def.stackLimit) {
				SlotGroup slotGroup = __instance.PositionHeld.GetSlotGroup (__instance.MapHeld);
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
	}
}
