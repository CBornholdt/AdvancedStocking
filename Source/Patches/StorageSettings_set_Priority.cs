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
    static public class StorageSettings_set_Priority
    {
        public static void Prefix(StorageSettings __instance, ref StoragePriority value)
        {
            Building_Shelf shelf = __instance.owner as Building_Shelf;
            if(value != __instance.Priority)
                shelf?.Notify_PriorityChanging(value);
        }
    }
}
