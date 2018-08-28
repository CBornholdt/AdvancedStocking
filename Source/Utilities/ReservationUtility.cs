using System;
using RimWorld;
using Verse.AI;
using Verse;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Harmony;

namespace AdvancedStocking
{
    static public class ReservationUtility
    {
        static public List<IntVec3> cellsToBeHighlighted = new List<IntVec3>();

        static public void HighlightCells()
        {
            foreach(var cell in cellsToBeHighlighted)
                HighlightReservationAt(cell);

            cellsToBeHighlighted.Clear();
        }
    
        static public void HighlightReservationAt(IntVec3 cell)
        {
            Vector3 position = cell.ToVector3ShiftedWithAltitude (AltitudeLayer.MetaOverlays);
            GenDraw.CurTargetingMat.SetPass(0);
            Graphics.DrawMeshNow (MeshPool.plane10, position, Quaternion.identity);
        }
    }
}
