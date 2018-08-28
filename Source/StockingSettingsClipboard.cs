using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace AdvancedStocking
{
    public static class StockingSettingsClipboard
    {
        private static Building_Shelf copiedShelf = null;
    
        private static bool copied = false;

        private static bool setupCallback = false;

        public static void Copy(Building_Shelf shelf)
        {
            StorageSettingsClipboard.Copy(shelf.settings);
            StockingSettingsClipboard.copiedShelf = shelf;
            StockingSettingsClipboard.copied = true;
            
            //As both this and StorageSettingsClipboard are static without constructors
            //  I have a timing issue setting up callbacks. I will do so during the first copy
            if (!setupCallback) 
                SetupCallback();
        }

        public static void CopyPasteGizmosFor_Postfix(StorageSettings s, ref IEnumerable<Gizmo> __result)
        {
            if(s.owner is Building_Shelf shelf){
                var result = new List<Gizmo>(2);
                result.Add(CopyGizmoFor(shelf));
                if (StockingSettingsClipboard.copied)
                    result.Add(PasteGizmoFor(shelf));
                else
                    result.Add(__result.ElementAt(1));  //keep Storage paste gizmo
                __result = result;
            }
        }

        private static Gizmo CopyGizmoFor(Building_Shelf shelf)
        {
            return new Command_Action() {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings", true),
                defaultLabel = "CommandCopyStockingSettingsLabel".Translate(),
                defaultDesc = "CommandCopyStockingSettingsDesc".Translate(),
                action = delegate {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    StockingSettingsClipboard.Copy(shelf);
                },
                hotKey = KeyBindingDefOf.Misc4
            };
        }

        private static Gizmo PasteGizmoFor(Building_Shelf shelf)
        {
            return new Command_Action() {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings", true),
                defaultLabel = "CommandPasteStockingSettingsLabel".Translate(),
                defaultDesc = "CommandPasteStockingSettingsDesc".Translate(),
                action = delegate {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    StockingSettingsClipboard.PasteInto(shelf);
                },
                hotKey = KeyBindingDefOf.Misc5
            };
        }

        public static void PasteInto(Building_Shelf shelf)
        {
            shelf.CopyStockSettingsFrom (StockingSettingsClipboard.copiedShelf);
        }

        //Whenever something is copied to other clipboard, it will reset this one
        private static void SetupCallback()
        {
            FieldInfo copiedClipboardSettingsField = typeof(StorageSettingsClipboard).GetField("clipboard", BindingFlags.NonPublic | BindingFlags.Static);
            var clipboardSettings = (StorageSettings)copiedClipboardSettingsField.GetValue(null);
            FieldInfo settingsChangedCallbackField = typeof(ThingFilter).GetField("settingsChangedCallback", BindingFlags.NonPublic | BindingFlags.Instance);
            Action oldSettingsChangedCallback = (Action)settingsChangedCallbackField.GetValue(clipboardSettings.filter);
            settingsChangedCallbackField.SetValue(clipboardSettings.filter, new Action(() => {
                oldSettingsChangedCallback?.Invoke();
                StockingSettingsClipboard.copied = false;
            }));
            StockingSettingsClipboard.setupCallback = true;
        }
    }
}

