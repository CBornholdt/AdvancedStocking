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
	[HarmonyPatch(typeof(Verse.ThingUtility))]
	[HarmonyPatch("TryAbsorbStackNumToTake")]
	static class ThingUtility_TryAbsorbStackNumToTake
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), "stackLimit");
			MethodInfo helper = AccessTools.Method(typeof(ThingUtility_TryAbsorbStackNumToTake), 
				nameof(ThingUtility_TryAbsorbStackNumToTake.TransformStacklimitIfOnShelf));
			bool patched = false;

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField && !patched) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 2 and returns int
					patched = true;
				}
			}
		}

		static int TransformStacklimitIfOnShelf(int stackLimit, Thing thing)
		{
			Map map = thing.MapHeld;
			if (map == null || !thing.PositionHeld.InBounds(map))
				return stackLimit;
			SlotGroup slotGroup = thing.PositionHeld.GetSlotGroup(thing.MapHeld);
			if (slotGroup != null && slotGroup.parent != null && slotGroup.parent is Building_Shelf shelf)
				return shelf.GetStackLimit(thing);
			return stackLimit;
		}		
	}
}
