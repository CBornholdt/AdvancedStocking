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
	[HarmonyPatch(typeof(Verse.GenPlace))]
	[HarmonyPatch("TryPlaceDirect")]
	static class GenPlace_TryPlaceDirect
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), "stackLimit");
			MethodInfo helper = AccessTools.Method(typeof(GenPlace_TryPlaceDirect), 
				nameof(GenPlace_TryPlaceDirect.TransformStacklimitIfDestIsShelf));

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Ldarg_1);  //int, Thing, IntVec3 on stack
					yield return new CodeInstruction(OpCodes.Ldarg_2);  //int, Thing, IntVec3, Map on statck
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 4 and returns int
				}
			}
		}

		//Have the map, might as well save the lookup ...
		static int TransformStacklimitIfDestIsShelf(int stackLimit, Thing thing, IntVec3 cell, Map map)
		{
			SlotGroup slotGroup = cell.GetSlotGroup(map);
			if (slotGroup?.parent != null && slotGroup.parent is Building_Shelf shelf)
				return shelf.GetStackLimit(thing);
			return stackLimit;
		}		
	}
}
