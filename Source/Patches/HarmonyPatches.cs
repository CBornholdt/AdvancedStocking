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
	[StaticConstructorOnStartup]
	static class HarmonyPatches
	{
		static HarmonyPatches() {
		//	HarmonyInstance.DEBUG = true;
		
			HarmonyInstance harmony = HarmonyInstance.Create ("rimworld.furiouslyeloquent.advancedstocking");

			harmony.Patch (AccessTools.Method (typeof(Verse.Thing), nameof(Thing.TryAbsorbStack)), null, 
				new HarmonyMethod (typeof(Thing_TryAbsorbStack).GetMethod("Postfix")), null);

			harmony.Patch(AccessTools.Method(typeof(Verse.Thing), nameof(Thing.SpawnSetup)), null, null, 
				new HarmonyMethod(typeof(Thing_SpawnSetup).GetMethod("Transpiler")));

			harmony.Patch(AccessTools.Property(typeof(RimWorld.StorageSettings), nameof(StorageSettings.Priority)).GetSetMethod(),
				new HarmonyMethod(typeof(StorageSettings_set_Priority).GetMethod("Prefix")), null, null);
        
			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettingsClipboard), nameof(StorageSettingsClipboard.CopyPasteGizmosFor)), null, 
				new HarmonyMethod (typeof(AdvancedStocking.StockingSettingsClipboard).GetMethod("CopyPasteGizmosFor_Postfix")), null);
               
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))
                , new HarmonyMethod(typeof(AdvancedStocking.ApparelTracker_Wear).GetMethod("Prefix")), null, null);
       
            harmony.Patch(AccessTools.Method(typeof(Verse.CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))
                , null, new HarmonyMethod (typeof(CompressibilityDeciderUtility_IsSaveCompressible).GetMethod("Postfix")), null);

            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")
                , null, new HarmonyMethod (typeof(FloatMenuMakerMap_AddHumanlikeOrders).GetMethod("Postfix")), null);
                
            harmony.Patch(AccessTools.Method(typeof(Verse.GenPlace), "TryPlaceDirect"), null, null, 
                new HarmonyMethod(typeof(GenPlace_TryPlaceDirect).GetMethod("Transpiler")));

            harmony.Patch(AccessTools.Method(typeof(Verse.AI.HaulAIUtility), nameof(Verse.AI.HaulAIUtility.HaulToCellStorageJob)), null, null, 
                new HarmonyMethod(typeof(HaulAIUtility_HaulToCellStorageJob).GetMethod("Transpiler")));
                
            harmony.Patch(AccessTools.Method(typeof(Verse.RecipeWorkerCounter), nameof(RecipeWorkerCounter.CountProducts)), null, null, 
                new HarmonyMethod(typeof(RecipeWorkerCounter_CountProducts).GetMethod("Transpiler")));

            harmony.Patch(AccessTools.Method(typeof(RimWorld.StoreUtility), "NoStorageBlockersIn"), null, null, 
                new HarmonyMethod(typeof(StoreUtility_NoStorageBlockersIn).GetMethod("Transpiler")));  
                
            harmony.Patch(AccessTools.Method(typeof(Verse.TerrainGrid), "DoTerrainChangedEffects")
                , null, new HarmonyMethod (typeof(TerrainGrid_DoTerrainChangedEffects).GetMethod("Postfix")), null);
   
            harmony.Patch(AccessTools.Method(typeof(Verse.ThingUtility), nameof(ThingUtility.TryAbsorbStackNumToTake)), null, null, 
                new HarmonyMethod(typeof(ThingUtility_TryAbsorbStackNumToTake).GetMethod("Transpiler")));
		}
	}
}
