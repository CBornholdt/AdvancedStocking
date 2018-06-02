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
	[HarmonyPatch(typeof(Verse.AI.HaulAIUtility))]
	[HarmonyPatch(nameof(Verse.AI.HaulAIUtility.HaulMaxNumToCellJob))]
	static class HaulAIUtility_HaulMaxNumToCellJob
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), "stackLimit");
			MethodInfo helper = AccessTools.Method(typeof(HaulAIUtility_HaulMaxNumToCellJob), 
				nameof(HaulAIUtility_HaulMaxNumToCellJob.TransformStacklimitIfDestIsShelf));

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
					yield return new CodeInstruction(OpCodes.Ldarg_1);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Ldloc_1);	//int, Thing, SlotGroup on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 3 and returns int
				}
			}
		}

		static int TransformStacklimitIfDestIsShelf(int stackLimit, Thing thing, SlotGroup slotGroup)
		{
			if (slotGroup.parent != null && slotGroup.parent is Building_Shelf shelf)
				return shelf.GetStackLimit(thing);
			return stackLimit;
		}		
	}
}
