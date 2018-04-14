using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static void Copy(Building_Shelf shelf)
		{
			StockingSettingsClipboard.copiedShelf = shelf;
			StockingSettingsClipboard.copied = true;
		}

		public static bool CopyPasteGizmosFor_Prefix(StorageSettings s, ref IEnumerable<Gizmo> __result)
		{
			Building_Shelf shelf = s.owner as Building_Shelf;
			if(shelf != null && shelf.InStockingMode){
				__result = CopyPasteStockingGizmosFor (shelf);
				return false;
			}
			return true;
		}

		private static IEnumerable<Gizmo> CopyPasteStockingGizmosFor(Building_Shelf shelf)
		{
			yield return new Command_Action {
				icon = ContentFinder<Texture2D>.Get ("UI/Commands/CopySettings", true),
				defaultLabel = "CommandCopyStockingSettingsLabel".Translate (),
				defaultDesc = "CommandCopyStockingSettingsDesc".Translate (),
				action = delegate {
					SoundDefOf.TickHigh.PlayOneShotOnCamera (null);
					StockingSettingsClipboard.Copy (shelf);
				},
				hotKey = KeyBindingDefOf.Misc4
			};
			Command_Action paste = new Command_Action ();
			paste.icon = ContentFinder<Texture2D>.Get ("UI/Commands/PasteSettings", true);
			paste.defaultLabel = "CommandPasteStockingSettingsLabel".Translate ();
			paste.defaultDesc = "CommandPasteStockingSettingsDesc".Translate ();
			paste.action = delegate {
				SoundDefOf.TickHigh.PlayOneShotOnCamera (null);
				StockingSettingsClipboard.PasteInto (shelf);
			};
			paste.hotKey = KeyBindingDefOf.Misc5;
			if (!StockingSettingsClipboard.copied) {
				paste.Disable (null);
			}
			yield return paste;
		}

		public static void PasteInto(Building_Shelf shelf)
		{
			shelf.CopyStockSettingsFrom (StockingSettingsClipboard.copiedShelf);
		}
	}
}

