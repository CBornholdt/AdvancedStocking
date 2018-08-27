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
		//	HarmonyInstance.DEBUG = true;
		
			HarmonyInstance harmony = HarmonyInstance.Create ("rimworld.advancedstocking");

			harmony.Patch (AccessTools.Method (typeof(Verse.Thing), "TryAbsorbStack"), null, 
				new HarmonyMethod (typeof(Thing_TryAbsorbStack).GetMethod("Postfix")), null);

			harmony.Patch(AccessTools.Method(typeof(Verse.Thing), "SpawnSetup"), null, null, 
				new HarmonyMethod(typeof(AdvancedStocking.HarmonyPatches).GetMethod("SpawnSetup_Transpiler")));

			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettings), "set_Priority"),
				new HarmonyMethod(typeof(AdvancedStocking.HarmonyPatches).GetMethod("set_Priority_Prefix")), null, null);

			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettingsClipboard), "CopyPasteGizmosFor"), null, 
				new HarmonyMethod (typeof(AdvancedStocking.StockingSettingsClipboard).GetMethod("CopyPasteGizmosFor_Postfix")), null);
                
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))
                , new HarmonyMethod(typeof(AdvancedStocking.ApparelTracker_Wear).GetMethod("Prefix")), null, null);
                
            harmony.Patch(AccessTools.Method(typeof(Verse.CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))
                , null, new HarmonyMethod (typeof(CompressibilityDeciderUtility_IsSaveCompressible).GetMethod("Postfix")), null);
                
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")
                , null, new HarmonyMethod (typeof(FloatMenuMakerMap_AddHumanlikeOrders).GetMethod("Postfix")), null);

			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static IEnumerable<CodeInstruction> SpawnSetup_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = instructions.ToList ();
			FieldInfo thingDefStacklimit = AccessTools.Field (typeof(ThingDef), nameof(ThingDef.stackLimit));
			MethodInfo spawnSetupHelper = AccessTools.Method (typeof(HarmonyPatches), 
										nameof(HarmonyPatches.SpawnSetupHelper_IgnoreOverstack));

			for(int i = 0; i < codes.Count; i++) {

				if((i + 1) < codes.Count && codes[i].opcode == OpCodes.Ldfld && codes[i].operand == thingDefStacklimit 
					&& codes[i + 1].opcode == OpCodes.Ble) {
					yield return codes [i];
					yield return codes [i + 1];
					yield return new CodeInstruction (OpCodes.Ldarg_0);	//Leave Thing on stack
					yield return new CodeInstruction(OpCodes.Ldarg_1); //Leave Thing, Map on stack
					yield return new CodeInstruction (OpCodes.Ldarg_2); //Leave Thing, Map, Bool on stack
					yield return new CodeInstruction (OpCodes.Call, spawnSetupHelper);	//Consume 3, leave bool
					yield return new CodeInstruction (OpCodes.Brtrue, codes[i+1].operand); //Consume bool
					i++;
				}
				else
					yield return codes [i];
			}
		}

		//This will get called after a large butcher job or when the pawn attempts to place a larger than normal
		//	stackSize from inventory onto the stack
		//If true, will Ignore the Overstack check in in SpawnSetup
		public static bool SpawnSetupHelper_IgnoreOverstack(Thing thing, Map map, bool respawningAfterLoad)
		{
			if (respawningAfterLoad) {
				Current.Game.GetComponent<StockingGameComponent>().AddThingForOverstackCheck(thing);
				return true;
			}
			else
				return thing.PositionHeld.GetSlotGroup(map).parent is Building_Shelf;
		}

		public static void set_Priority_Prefix(StorageSettings __instance, ref StoragePriority value)
		{
			Building_Shelf shelf = __instance.owner as Building_Shelf;
			if(value != __instance.Priority)
				shelf?.Notify_PriorityChanging(value);
		}
	}
}
