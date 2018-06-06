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
	[HarmonyPatch(typeof(RecipeWorkerCounter))]
	[HarmonyPatch(nameof(RecipeWorkerCounter.CountProducts))]
	static class RecipeWorkerCounter_CountProducts
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo listThingCount = AccessTools.Property(typeof(List<Thing>), "Count").GetGetMethod(); 
			MethodInfo helper = AccessTools.Method(typeof(RecipeWorkerCounter_CountProducts), 
				nameof(RecipeWorkerCounter_CountProducts.ReplaceCountWithStackCount));

			foreach (var code in instructions) {
				if (code.opcode == OpCodes.Callvirt && code.operand == listThingCount)
					yield return new CodeInstruction(OpCodes.Call, helper);
				else
					yield return code;
			}
		}

		static int ReplaceCountWithStackCount(IEnumerable<Thing> things)
		{
			int count = things.Sum(thing => thing.stackCount);
			Log.Message("Count = " + count);
			return count;
		}		
	}
}
