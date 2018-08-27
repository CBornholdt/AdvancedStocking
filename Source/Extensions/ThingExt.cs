using System;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
    static public class ThingExt
    {
        static public Building_Shelf GetShelf(this Thing thing) =>
            thing.GetSlotGroup()?.parent as Building_Shelf;
    }
}
