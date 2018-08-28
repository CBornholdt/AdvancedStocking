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
	static class ThingUtility_TryAbsorbStackNumToTake
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.stackLimit));
			MethodInfo helper = AccessTools.Method(typeof(ThingUtility_TryAbsorbStackNumToTake), 
				nameof(ThingUtility_TryAbsorbStackNumToTake.TransformStacklimitIfOnShelf));

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 2 and returns int
				}
			}
		}

        //Not replacing with Reservation based StackLimits to prevent errors if stackLimit is returned as 0
		static int TransformStacklimitIfOnShelf(int stackLimit, Thing thing)
		{
			Map map = thing.MapHeld;
			if (map == null || !thing.PositionHeld.InBounds(map))
				return stackLimit;
            //Log.Message($"Stacklimit for {thing.Label} = { thing.GetShelf()?.GetStackLimit(thing) ?? stackLimit }");
            return thing.GetShelf()?.GetStackLimit(thing) ?? stackLimit;
		}		
	}
}
