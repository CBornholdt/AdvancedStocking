using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimWorld
{
	public static class ExtensionWidgets
	{
		public static void FloatMenuButtonOverEnum<T>(Rect rect, string buttonLabel, Action<T> selectedAction, Func<T, bool> displayPredicate = null, 
			bool drawBackground = true, bool isActive = true) where T : struct, IConvertible
		{
			if(Widgets.ButtonText(rect, buttonLabel, drawBackground, false, isActive)) {
				List<FloatMenuOption> list = new List<FloatMenuOption> ();
				foreach(T value in Enum.GetValues(typeof(T)).Cast<T>().Where(v => displayPredicate?.Invoke(v) ?? true))
					list.Add(new FloatMenuOption(Enum.GetName(typeof(T), value).Translate().CapitalizeFirst(), () => selectedAction(value),
						MenuOptionPriority.Default, null, null, 0, null, null));
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}					
	}
}
