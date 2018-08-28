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
    static class GenPlace_HaulPlaceBlockerIn
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo stackLimitField = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.stackLimit));
            MethodInfo helper = AccessTools.Method(typeof(GenPlace_HaulPlaceBlockerIn), 
                nameof(GenPlace_HaulPlaceBlockerIn.TransformStacklimitIfDestIsShelf));

            foreach (var code in instructions) {
                yield return code;
                if (code.opcode == OpCodes.Ldfld && code.operand == stackLimitField) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);  //int, Thing on stack
                    yield return new CodeInstruction(OpCodes.Ldarg_1);  //int, Thing, IntVec3 on stack
                    yield return new CodeInstruction(OpCodes.Call, helper); //Consumes 3 and returns int
                }
            }
        }

        //Have the map, might as well save the lookup ...
        static int TransformStacklimitIfDestIsShelf(int stackLimit, Thing thing, IntVec3 cell)
        {
            return thing.GetShelf()?.GetStackLimit(thing, cell) ?? stackLimit;
        }       
    }
}
