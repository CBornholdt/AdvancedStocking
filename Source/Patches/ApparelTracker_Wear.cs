using Verse;
using Verse.AI;
using RimWorld;
using Harmony;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace AdvancedStocking
{
    static public class ApparelTracker_Wear
    {
        static public void Prefix(ref Apparel newApparel)
        {
            if (newApparel.stackCount > 1)
                newApparel = (Apparel)newApparel.SplitOff(1);
        }
    }
}
