using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AdvancedStocking
{
    public class TreeNode_UIOption_ReservedItem : TreeNode_UIOption
    {
        Building_Shelf shelf;
        ThingDef reservedThing;
        IntVec3 cell;

        public TreeNode_UIOption_ReservedItem(Building_Shelf shelf, ThingDef reservedThing, IntVec3 cell, bool forcedOpen = false) 
            : base(reservedThing.LabelCap, "AS.ReservedItem.ToolTip".Translate(cell.ToString()), forcedOpen, () => true)
        {
            this.shelf = shelf;
            this.reservedThing = reservedThing;
            this.cell = cell;
        }

        public override float Draw(Rect area, float lineHeight)
        {
            int textHeight = (int) Text.CalcHeight(this.label, area.width - lineHeight);
            //Set height of area to be integral units of lineHeight
            area.height = (textHeight % (int)lineHeight == 0) ? textHeight : textHeight + ((int)lineHeight - textHeight % (int)lineHeight);
            Widgets.DrawHighlightIfMouseover (area);
            
            Rect labelRect = new Rect(area);
            labelRect.width -= lineHeight;
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label (labelRect, this.label);
            Text.Anchor = anchor;

            if (Mouse.IsOver (area)) {
                GUI.DrawTexture (area, TexUI.HighlightTex);
                ReservationUtility.cellsToBeHighlighted.Add(cell);
                if (!this.tipText.NullOrEmpty ()) {
                    TooltipHandler.TipRegion (area, this.tipText);
                }  
            }

            Rect removeReservationRect = new Rect(area.xMax - lineHeight, area.yMin, lineHeight, lineHeight);
            if(Widgets.CloseButtonFor(removeReservationRect)) {
                SoundDefOf.TinyBell.PlayOneShotOnCamera();
                shelf.RemoveCellReservation(cell);
                this.Remove();
            }

            return area.yMax;
        }
    }
}

