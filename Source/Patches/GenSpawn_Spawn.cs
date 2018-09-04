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
    static public class GenSpawn_Spawn
    {
        static public IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo thingDefCategory = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.category));

            foreach(var code in instructions) {
                yield return code;
                if(code.opcode == OpCodes.Ldfld && code.operand == thingDefCategory) {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, (int)ThingCategory.None);
                }
            }   
        }
    }
}
