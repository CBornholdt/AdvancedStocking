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
	static class StoreUtility_NoStorageBlockersIn
	{
        static int i = 0;
    
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.stackLimit));
			MethodInfo helper = AccessTools.Method(typeof(StoreUtility_NoStorageBlockersIn), 
				nameof(StoreUtility_NoStorageBlockersIn.TransformStacklimitIfCellIsAShelf));
            
            MethodInfo note = AccessTools.Method(typeof(StoreUtility_NoStorageBlockersIn), 
                nameof(StoreUtility_NoStorageBlockersIn.Note));

            //yield return new CodeInstruction(OpCodes.Call, note);

			foreach (var code in instructions) {
				yield return code;
				if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
					yield return new CodeInstruction(OpCodes.Ldarg_2);  //int, Thing on stack
					yield return new CodeInstruction(OpCodes.Ldarg_0);	//int, Thing, IntVec3 on stack
					yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 3 and returns int
				}
			}
		}

        static void Note()
        {      
            Log.Message("starting StoreUtility_NoStorageBlockersIn " + (i++).ToString());
        }

		//If cell to checked for storage blockers is a shelf, return enhanced stackLimits
		static int TransformStacklimitIfCellIsAShelf(int stackLimit, Thing thing, IntVec3 cell)
		{
			Map map = thing.MapHeld;
			if (map == null)	//Is occasionally called on newly created items before they get a map ...
				return stackLimit;
            return cell.GetShelf(map)?.GetStackLimit(thing, cell) ?? stackLimit;
		}		
	}
}
