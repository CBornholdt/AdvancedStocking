using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace AdvancedStocking
{
	[HarmonyPatch(typeof(RimWorld.StoreUtility))]
	[HarmonyPatch("NoStorageBlockersIn")]
	static class StoreUtility_NoStorageBlockersIn
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), "stackLimit");
			MethodInfo helper = AccessTools.Method(typeof(StoreUtility_NoStorageBlockersIn), "Helper");
			bool patched = false;

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField && !patched) {
					yield return new CodeInstruction(OpCodes.Ldarg_2);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Ldarg_0);	//int, Thing, IntVec3 on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 3 and returns int
					patched = true;
				}
			}
		}

		//If cell to checked for storage blockers is a shelf, return enhanced stackLimits
		static int Helper(int stackLimit, Thing thing, IntVec3 cell)
		{
			Map map = thing.MapHeld;
			if (map == null)	//Is occasionally called on newly created items before they get a map ...
				return stackLimit;
			SlotGroup slotGroup = cell.GetSlotGroup(map);
			if (slotGroup != null && slotGroup.parent != null && slotGroup.parent is Building_Shelf shelf)
				return shelf.GetStackLimit(thing);
			return stackLimit;
		}		
	}
}
