using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AdvancedStocking
{
	public class TreeNode_UIOption : TreeNode
	{
		//Area to draw, line height, return used height
		public Func<Rect, float, float> drawFunc = null;
		public string label;
		public bool forcedOpen = false;
		public Func<bool> isActive = null;

		public TreeNode_UIOption(Func<Rect, float,float> drawFunc, string label, bool forcedOpen = false, Func<bool> isActive = null) 
			: this(label, forcedOpen, isActive)
		{
			this.drawFunc = drawFunc;
		}

		protected TreeNode_UIOption(string label, bool forcedOpen = false, Func<bool> isActive = null) : base()
		{
			this.label = label;
			this.forcedOpen = forcedOpen;
			this.isActive = isActive;
			children = new List<TreeNode>();
		}

		public override string ToString () => label;

		public virtual float Draw(Rect area, float lineHeight) => drawFunc(area, lineHeight);
	}

	public class TreeNode_UIOptionCheckbox : TreeNode_UIOption
	{
		private Func<bool> getter;
		private Action<bool> setter;
		private string tipText;

		public TreeNode_UIOptionCheckbox(string name, Func<bool> getter, Action<bool> setter, string tipText = null, bool forcedOpen = false, Func<bool> isActive = null) 
			: base(name, forcedOpen, isActive)
		{
			this.getter = getter;
			this.setter = setter;
			this.tipText = tipText;
		}

		public override float Draw(Rect area, float lineHeight)
		{
			int textHeight = (int) Text.CalcHeight(this.label, area.width - lineHeight);
			//Set height of area to be integral units of lineHeight
			area.height = (textHeight % (int)lineHeight == 0) ? (float)textHeight : ((textHeight / (int)lineHeight) + 1) * lineHeight;
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
					SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera (null);
				}
				else {
					SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera (null);
				}
			}

			Rect cbRect = new Rect(area.xMax - lineHeight, area.yMin, lineHeight, lineHeight);
			bool value = getter();
			Widgets.Checkbox(cbRect.xMin, cbRect.yMin, ref value, lineHeight, (!this.isActive?.Invoke() ?? false));
			setter(value);

			return labelRect.yMax;
		}
	}
}
