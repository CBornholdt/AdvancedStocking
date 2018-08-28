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
	static class HaulAIUtility_HaulToCellStorageJob
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), "stackLimit");
			MethodInfo helper = AccessTools.Method(typeof(HaulAIUtility_HaulToCellStorageJob), 
				nameof(HaulAIUtility_HaulToCellStorageJob.TransformStacklimitIfDestIsShelf));

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
					yield return new CodeInstruction(OpCodes.Ldarg_1);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Ldloc_1);	//int, Thing, SlotGroup on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 3 and returns int
				}
			}
		}

        //I ignore reservations here, as the logic should have already prevented the Job via StoreUtility.NoStorageBlockersIn
        //  and Setting the count to 0 here will cause errors ... I may need to revisit this
		static int TransformStacklimitIfDestIsShelf(int stackLimit, Thing thing, SlotGroup slotGroup)
		{
			if (slotGroup.parent != null && slotGroup.parent is Building_Shelf shelf)
				return shelf.GetStackLimit(thing);
			return stackLimit;
		}		
	}
}
