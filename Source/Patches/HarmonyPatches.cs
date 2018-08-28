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
            HarmonyInstance.DEBUG = true;
		
			HarmonyInstance harmony = HarmonyInstance.Create ("rimworld.furiouslyeloquent.advancedstocking");

			harmony.Patch (AccessTools.Method (typeof(Verse.Thing), nameof(Thing.TryAbsorbStack)), null, 
				new HarmonyMethod (typeof(Thing_TryAbsorbStack), "Postfix"), null);

			harmony.Patch(AccessTools.Method(typeof(Verse.Thing), nameof(Thing.SpawnSetup)), null, null, 
				new HarmonyMethod(typeof(Thing_SpawnSetup), "Transpiler"));

			harmony.Patch(AccessTools.Property(typeof(RimWorld.StorageSettings), nameof(StorageSettings.Priority)).GetSetMethod(),
				new HarmonyMethod(typeof(StorageSettings_set_Priority), "Prefix"), null, null);
        
			harmony.Patch(AccessTools.Method(typeof(RimWorld.StorageSettingsClipboard), nameof(StorageSettingsClipboard.CopyPasteGizmosFor)), null, 
				new HarmonyMethod (typeof(AdvancedStocking.StockingSettingsClipboard), "CopyPasteGizmosFor_Postfix"), null);
               
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))
                , new HarmonyMethod(typeof(AdvancedStocking.ApparelTracker_Wear), "Prefix"), null, null);
       
            harmony.Patch(AccessTools.Method(typeof(Verse.CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))
                , null, new HarmonyMethod (typeof(CompressibilityDeciderUtility_IsSaveCompressible), "Postfix"), null);

            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")
                , null, new HarmonyMethod (typeof(FloatMenuMakerMap_AddHumanlikeOrders), "Postfix"), null);
                
            harmony.Patch(AccessTools.Method(typeof(Verse.GenPlace), nameof(GenPlace.HaulPlaceBlockerIn)), null, null, 
                new HarmonyMethod(typeof(GenPlace_HaulPlaceBlockerIn), "Transpiler"));
                
            harmony.Patch(AccessTools.Method(typeof(Verse.GenPlace), "TryPlaceDirect"), null, null, 
                new HarmonyMethod(typeof(GenPlace_TryPlaceDirect), "Transpiler"));
            
            //For logging/debugging    
         /*   harmony.Patch(AccessTools.Method(typeof(Verse.GenPlace), "TryPlaceThing", new Type[7] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>), typeof(Predicate<IntVec3>) }), null, 
                new HarmonyMethod(typeof(GenPlace_TryPlaceThing), nameof(GenPlace_TryPlaceThing.Postfix)), null);   */

            harmony.Patch(AccessTools.Method(typeof(Verse.AI.HaulAIUtility), nameof(Verse.AI.HaulAIUtility.HaulToCellStorageJob)), null, null, 
                new HarmonyMethod(typeof(HaulAIUtility_HaulToCellStorageJob), "Transpiler"));
                
            harmony.Patch(AccessTools.Method(typeof(Verse.RecipeWorkerCounter), nameof(RecipeWorkerCounter.CountProducts)), null, null, 
                new HarmonyMethod(typeof(RecipeWorkerCounter_CountProducts), "Transpiler"));

            harmony.Patch(AccessTools.Method(typeof(RimWorld.StoreUtility), "NoStorageBlockersIn"), null, null, 
                new HarmonyMethod(typeof(StoreUtility_NoStorageBlockersIn), "Transpiler"));  
                
            harmony.Patch(AccessTools.Method(typeof(Verse.TerrainGrid), "DoTerrainChangedEffects")
                , null, new HarmonyMethod (typeof(TerrainGrid_DoTerrainChangedEffects), "Postfix"), null);
   
            harmony.Patch(AccessTools.Method(typeof(Verse.ThingUtility), nameof(ThingUtility.TryAbsorbStackNumToTake)), null, null, 
                new HarmonyMethod(typeof(ThingUtility_TryAbsorbStackNumToTake), "Transpiler"));
		}
	}
}
