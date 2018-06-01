using System;
using Verse;
using UnityEngine;

namespace AdvancedStocking
{
	public class TreeNode_UIOption_Slider : TreeNode_UIOption
    {
		Func<float> valGetter;
		Action<float> valSetter;
		Func<float> maxGetter;
		Func<float> minGetter;
		float roundTo;
		Func<string> labelGetter;

		public TreeNode_UIOption_Slider(string label, Func<float> valGetter, Action<float> valSetter, Func<float> minGetter
										, Func<float> maxGetter, float roundTo = 1f
		                                , string toolTip = null, bool forcedOpen = false, Func<bool> isActive = null)
			: base(label, toolTip, forcedOpen, isActive)
        {
			this.valGetter = valGetter;
			this.valSetter = valSetter;
			this.minGetter = minGetter;
			this.maxGetter = maxGetter;
			this.roundTo = roundTo;
			this.labelGetter = null;
        }
        
        public TreeNode_UIOption_Slider(Func<string> labelGetter, Func<float> valGetter, Action<float> valSetter, Func<float> minGetter
										, Func<float> maxGetter, float roundTo = 1f
		                                , string toolTip = null, bool forcedOpen = false, Func<bool> isActive = null)
			: base(null, toolTip, forcedOpen, isActive)
        {
			this.valGetter = valGetter;
			this.valSetter = valSetter;
			this.minGetter = minGetter;
			this.maxGetter = maxGetter;
			this.roundTo = roundTo;
			this.labelGetter = labelGetter;
        }

		public override float Draw(Rect area, float lineHeight)
		{
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.UpperCenter;
			GameFont font = Text.Font;
			Text.Font = GameFont.Small;
			Rect labelRect = new Rect(area);
			labelRect.height = lineHeight;
			Rect sliderRect = new Rect(labelRect);

			bool active = this.isActive?.Invoke() ?? true;
            float val = valGetter();

			string centerLabel = (this.labelGetter == null) ? this.label : this.labelGetter();
			Widgets.Label(labelRect, centerLabel + ": " + val);
			sliderRect.y += lineHeight;
			sliderRect.xMin += 5;
			sliderRect.xMax -= 5;

            //Disable the slider by setting min/max to val
			valSetter(Widgets.HorizontalSlider(sliderRect, val
			                                   , active ? minGetter() : val
			                                   , active ? maxGetter() : val
			                                   , false, null, null, null, roundTo: 1f));

			area.height = 2 * lineHeight;
			Widgets.DrawHighlightIfMouseover (area);

			if (!this.tipText.NullOrEmpty ()) {
				if (Mouse.IsOver (area)) {
					GUI.DrawTexture (area, TexUI.HighlightTex);
				}
				TooltipHandler.TipRegion (area, this.tipText);
			}
            
			Text.Anchor = anchor;
			Text.Font = font;
			return sliderRect.yMax;
		}
    }
}
                 