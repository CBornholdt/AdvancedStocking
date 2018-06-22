using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace AdvancedStocking
{
    static public class Listing_StandardExt
    {
        static public float Slider(this Listing_Standard listing, float val, float min, float max, float roundTo)
        {
            Rect rect = listing.GetRect(22f);
            float result = Widgets.HorizontalSlider(rect, val, min, max, false, null, null, null, roundTo);
            listing.Gap(listing.verticalSpacing);
            return result;
        }
    }
}
