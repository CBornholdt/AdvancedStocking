using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AdvancedStocking
{
	public class TreeNode_UIOption_Checkbox : TreeNode_UIOption
	{
		private Func<bool> getter;
		private Action<bool> setter;

		public TreeNode_UIOption_Checkbox(string name, Func<bool> getter, Action<bool> setter, string tipText = null, bool forcedOpen = false, Func<bool> isActive = null) 
			: base(name, tipText, forcedOpen, isActive)
		{
			this.getter = getter;
			this.setter = setter;
		}

		public override float Draw(Rect area, float lineHeight)
		{
			int textHeight = (int) Text.CalcHeight(this.label, area.width - lineHeight);
			//Set height of area to be integral units of lineHeight
			area.height = (textHeight % (int)lineHeight == 0) ? textHeight : textHeight + ((int)lineHeight - textHeight % (int)lineHeight);
			Widgets.DrawHighlightIfMouseover (area);

			if (!this.tipText.NullOrEmpty ()) {
				if (Mouse.IsOver (area)) {
					GUI.DrawTexture (area, TexUI.HighlightTex);
				}
				TooltipHandler.TipRegion (area, this.tipText);
			}

			Rect labelRect = new Rect(area);
			labelRect.width -= lineHeight;	
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label (labelRect, this.label);
			Text.Anchor = TextAnchor.UpperLeft;
			if ((this.isActive?.Invoke() ?? true) && Widgets.ButtonInvisible(labelRect)) {
				setter(!getter());
				if (getter()) {
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera (null);
				}
				else {
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera (null);
				}
			}

			Rect cbRect = new Rect(area.xMax - lineHeight, area.yMin, lineHeight, lineHeight);
			bool value = getter();
			Widgets.Checkbox(cbRect.xMin, cbRect.yMin, ref value, lineHeight, (!this.isActive?.Invoke() ?? false));
			setter(value);

			return area.yMax;
		}
	}
}

