using System;
using Verse;
using UnityEngine;

namespace AdvancedStocking
{
	public class TreeNode_UIOption_Slider : TreeNode_UIOption
    {
		Func<float> valGetter;
		Action<float> valSetter;
		float min;
		float max;
		float roundTo;

		public TreeNode_UIOption_Slider(string label, Func<float> valGetter, Action<float> valSetter, float min, float max, float roundTo = 1f
		                                , string toolTip = null, bool forcedOpen = false, Func<bool> isActive = null)
			: base(label, toolTip, forcedOpen, isActive)
        {
			this.valGetter = valGetter;
			this.valSetter = valSetter;
			this.min = min;
			this.max = max;
			this.roundTo = roundTo;
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

			Widgets.Label(labelRect, label + ": " + val);
			sliderRect.y += lineHeight;

            //Disable the slider by setting min/max to val
			valSetter(Widgets.HorizontalSlider(sliderRect, val
			                                   , active ? min : val
			                                   , active ? max : val
			                                   , false, null, null, null, roundTo: 1f));
            
			Text.Anchor = anchor;
			Text.Font = font;
			return sliderRect.yMax;
		}
    }
}
                 