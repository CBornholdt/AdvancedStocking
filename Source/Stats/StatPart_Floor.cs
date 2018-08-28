using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace AdvancedStocking
{
	public class StatPart_Floor : StatPart
    {
		float wetFactor = 1f;
		float diggableFactor = 1f;
		float growableFactor = 1f;
		float roughFactor = 1f;
		float metalFloorFactor = 1f;
		float stoneFloorFactor = 1f;
		float fabricFloorFactor = 1f;
		float woodFloorFactor = 1f;

        //Store these in class so that the explanation does not need to recalculate
		float percentageWet = 0;
		float percentageDiggable = 0;
		float percentageGrowable = 0;
		float percentageRough = 0;
		float percentageMetal = 0;
		float percentageStone = 0;
		float percentageWood = 0;
		float percentageFabric = 0;

		public override void TransformValue(StatRequest req, ref float val)
		{
			Map map = req.Thing.Map;
			List<TerrainDef> terrainUnderShelf = req.Thing.OccupiedRect().Cells
			                                        .Select(cell => map.terrainGrid.TerrainAt(cell))
			                                        .ToList();

		/*	int wetCount = 0;
			int diggableCount = 0;
			int growableCount = 0;
			int metalCount = 0;
			int stoneCount = 0;
            int 
			foreach (var terrain in terrainUnderShelf) {



			}   */
			percentageWet = (float)terrainUnderShelf.Count(terrain => terrain.driesTo != null) / (float)terrainUnderShelf.Count;
			percentageDiggable = (float)terrainUnderShelf
				.Count(terrain => terrain.affordances.Contains(TerrainAffordanceDefOf.Diggable)) / (float)terrainUnderShelf.Count;
			percentageGrowable = (float)terrainUnderShelf
				.Count(terrain => terrain.affordances.Contains(TerrainAffordanceDefOf.GrowSoil)) / (float)terrainUnderShelf.Count;
			percentageRough = (float)terrainUnderShelf
                .Count(terrain => !terrain.affordances.Contains(TerrainAffordanceDefOf.SmoothableStone)) / (float)terrainUnderShelf.Count;
			percentageMetal = (float)terrainUnderShelf
				.Count(terrain => terrain.costList?.First().thingDef.stuffProps?.categories.Contains(StuffCategoryDefOf.Metallic) ?? false)
				/ (float)terrainUnderShelf.Count;
			percentageStone = (float)terrainUnderShelf
                .Count(terrain => terrain.costList?.First().thingDef.stuffProps?.categories.Contains(StuffCategoryDefOf.Stony) ?? false)
                / (float)terrainUnderShelf.Count;
			percentageWood = (float)terrainUnderShelf
                .Count(terrain => terrain.costList?.First().thingDef.stuffProps?.categories.Contains(StuffCategoryDefOf.Woody) ?? false)
                / (float)terrainUnderShelf.Count;
			percentageFabric = (float)terrainUnderShelf
                .Count(terrain => terrain.costList?.First().thingDef.stuffProps?.categories.Contains(StuffCategoryDefOf.Fabric) ?? false)
                / (float)terrainUnderShelf.Count;

            //I need to adjust the factors proportionally around 1, not 0. So I need to adjust before and after
			if (percentageWet > 0 && wetFactor != 1f)
				val *= 1f + (percentageWet * (wetFactor - 1f));
			if (percentageDiggable > 0 && diggableFactor != 1f)
				val *= 1f + (percentageDiggable * (diggableFactor - 1f));
			if (percentageGrowable > 0 && growableFactor != 1f)
				val *= 1f + (percentageGrowable * (growableFactor - 1f));
			if (percentageRough > 0 && roughFactor != 1f)
                val *= 1f + (percentageRough * (roughFactor - 1f));
			if (percentageMetal > 0 && metalFloorFactor != 1f)
				val *= 1f + (percentageMetal * (metalFloorFactor - 1f));
			if (percentageStone > 0 && stoneFloorFactor != 1f)
				val *= 1f + (percentageStone * (stoneFloorFactor - 1f));
			if (percentageWood > 0 && woodFloorFactor != 1f)
				val *= 1f + (percentageWood * (woodFloorFactor - 1f));
			if (percentageFabric > 0 && fabricFloorFactor != 1f)
				val *= 1f + (percentageFabric * (fabricFloorFactor - 1f));
		}
        
		public override string ExplanationPart(StatRequest req)
		{
			var explanation = new StringBuilder();

			if (percentageWet > 0 && wetFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Wet".Translate(percentageWet.ToStringPercent()) + ": x"
				                       + (1f + (percentageWet * (wetFactor - 1f))).ToStringPercent());
            if (percentageDiggable > 0 && diggableFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Diggable".Translate(percentageDiggable.ToStringPercent()) + ": x"
				                       + (1f + (percentageDiggable * (diggableFactor - 1f))).ToStringPercent());
            if (percentageGrowable > 0 && growableFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Growable".Translate(percentageGrowable.ToStringPercent()) + ": x"
				                       + (1f + (percentageGrowable * (growableFactor - 1f))).ToStringPercent());
			if (percentageRough > 0 && roughFactor != 1f)
                explanation.AppendLine("StatReport_Floor.Rough".Translate(percentageRough.ToStringPercent()) + ": x"
                                       + (1f + (percentageRough * (roughFactor - 1f))).ToStringPercent());
            if (percentageMetal > 0 && metalFloorFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Metal".Translate(percentageMetal.ToStringPercent()) + ": x"
				                       + (1f + (percentageMetal * (metalFloorFactor - 1f))).ToStringPercent());
            if (percentageStone > 0 && stoneFloorFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Stone".Translate(percentageStone.ToStringPercent()) + ": x"
				                       + (1f + (percentageStone * (stoneFloorFactor - 1f))).ToStringPercent());
            if (percentageWood > 0 && woodFloorFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Wood".Translate(percentageWood.ToStringPercent()) + ": x"
				                       + (1f + (percentageWood * (woodFloorFactor - 1f))).ToStringPercent());
            if (percentageFabric > 0 && fabricFloorFactor != 1f)
				explanation.AppendLine("StatReport_Floor.Fabric".Translate(percentageFabric.ToStringPercent()) + ": x"
				                       + (1f + (percentageFabric * (fabricFloorFactor - 1f))).ToStringPercent());

			return (explanation.Length == 0) ? null : explanation.ToString();
		}
	}
}
