using System;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
    static public class CellExt
    {
        static public Building_Shelf GetShelf(this IntVec3 cell, Map map) =>
            cell.GetSlotGroup(map)?.parent as Building_Shelf;
    }
}
