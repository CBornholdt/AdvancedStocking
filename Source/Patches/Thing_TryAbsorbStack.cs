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
    static public class Thing_TryAbsorbStack
    {
        //TODO move this to a separate transpiler because I lose the stackCount added during the original method
        public static void Postfix(Thing __instance, Thing other, bool respectStackLimit, ref bool __result)
        {
            if (__instance == null || __instance.def.category != ThingCategory.Item || !__instance.Spawned)
                return;
            __instance.GetShelf()?.Notify_ReceivedMoreOfAThing(__instance, 0);
        }
    }
}
