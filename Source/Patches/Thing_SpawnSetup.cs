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
    static public class Thing_SpawnSetup
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList ();
            FieldInfo thingDefStacklimit = AccessTools.Field (typeof(ThingDef), nameof(ThingDef.stackLimit));
            MethodInfo spawnSetupHelper = AccessTools.Method (typeof(Thing_SpawnSetup), 
                                        nameof(Thing_SpawnSetup.Helper_IgnoreOverstack));

            for(int i = 0; i < codes.Count; i++) {

                if((i + 1) < codes.Count && codes[i].opcode == OpCodes.Ldfld && codes[i].operand == thingDefStacklimit 
                    && codes[i + 1].opcode == OpCodes.Ble) {
                    yield return codes [i];
                    yield return codes [i + 1];
                    yield return new CodeInstruction (OpCodes.Ldarg_0); //Leave Thing on stack
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //Leave Thing, Map on stack
                    yield return new CodeInstruction (OpCodes.Ldarg_2); //Leave Thing, Map, Bool on stack
                    yield return new CodeInstruction (OpCodes.Call, spawnSetupHelper);  //Consume 3, leave bool
                    yield return new CodeInstruction (OpCodes.Brtrue, codes[i+1].operand); //Consume bool
                    i++;
                }
                else
                    yield return codes [i];
            }
        }

        //This will get called after a large butcher job or when the pawn attempts to place a larger than normal
        //  stackSize from inventory onto the stack
        //If true, will Ignore the Overstack check in in SpawnSetup
        public static bool Helper_IgnoreOverstack(Thing thing, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) {
                Current.Game.GetComponent<StockingGameComponent>().AddThingForOverstackCheck(thing);
                return true;
            }
            else
                return thing.PositionHeld.GetSlotGroup(map).parent is Building_Shelf;
        }
    }
}

