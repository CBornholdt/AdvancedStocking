using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AdvancedStocking
{
	public class TreeNode_UIOption_EnumMenuButton<T> : TreeNode_UIOption where T : struct, IConvertible
	{
		private Action<T> selectedAction;
		private Func<T, bool> displayPredicate;
		private Func<string> buttonLabelMaker;
		private float buttonWidth;

		public TreeNode_UIOption_EnumMenuButton(string labelText, Func<string> buttonLabelMaker, Action<T> selectedAction, Func<T, bool> displayPredicate, 
			float buttonWidth = 0, string tipText = null, bool forcedOpen = false, Func<bool> isActive = null) : base(labelText, tipText, forcedOpen, isActive)
		{
			this.selectedAction = selectedAction;
			this.displayPredicate = displayPredicate;
			this.buttonLabelMaker = buttonLabelMaker;
			this.buttonWidth = buttonWidth;
		}

		public override float Draw(Rect area, float lineHeight)
		{
			if (this.buttonWidth == 0)
				this.buttonWidth = lineHeight * 3;

			GameFont savedFont = Text.Font;
			String buttonLabel = buttonLabelMaker ();
			Rect mbRect = new Rect (area.xMax - this.buttonWidth, area.yMin, this.buttonWidth, lineHeight);
			Text.Font = GameFont.Small;
			int buttonHeight = (int)Text.CalcHeight (buttonLabel, buttonWidth) + 4;	//Will need to determine how to get '6' from button properties instead
			mbRect.height = (buttonHeight % (int)lineHeight == 0) ? (float)buttonHeight : ((buttonHeight / (int)lineHeight) + 1) * lineHeight;

			Rect labelRect = new Rect (area);
			labelRect.width -= this.buttonWidth;
			Text.Font = GameFont.Medium;
			int textHeight = (int) Text.CalcHeight(this.label, labelRect.width);
			labelRect.height = (textHeight % (int)lineHeight == 0) ? (float)textHeight : ((textHeight / (int)lineHeight) + 1) * lineHeight;

			area.height = (labelRect.height >= mbRect.height) ? labelRect.height : mbRect.height;
			Widgets.DrawHighlightIfMouseover (area);

			if (!this.tipText.NullOrEmpty ()) {
				if (Mouse.IsOver (area)) {
					GUI.DrawTexture (area, TexUI.HighlightTex);
				}
				TooltipHandler.TipRegion (area, this.tipText);
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label (labelRect, this.label);
			Text.Anchor = TextAnchor.UpperLeft;

			Text.Font = GameFont.Small;
			ExtensionWidgets.FloatMenuButtonOverEnum<T> (mbRect, buttonLabel, this.selectedAction, 
				this.displayPredicate, true, (this.isActive?.Invoke() ?? true));
			Text.Font = savedFont;
			return area.yMax;
		}
	}
}
