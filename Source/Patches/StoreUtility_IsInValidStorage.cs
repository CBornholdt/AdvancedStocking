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
    static public class StoreUtility_IsInValidStorage
    {
        static public void Postfix(bool __result, Thing t)
        {
            __result = __result && !(t.GetShelf()?.IsCellReservedByOtherThing(t.PositionHeld, t.def) ?? true);
        }
    }
}
