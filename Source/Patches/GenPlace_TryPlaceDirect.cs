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
	public static class GenPlace_TryPlaceDirect
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
		public static int TransformStacklimitIfDestIsShelf(int stackLimit, Thing thing, IntVec3 cell, Map map)
		{
            return cell.GetShelf(map)?.GetStackLimit(thing, cell) ?? stackLimit;
		}		
	}
    
    public static class GenPlace_TryPlaceThing
    {
        static public void Postfix(bool __result, Thing thing, IntVec3 center, Map map, ThingPlaceMode mode)
        {
            Log.Message($"TryPlaceThing {thing.ToString()} in cell { center } using mode { mode } with result { __result }");
        }
    }  
}
