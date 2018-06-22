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
        string editBuffer;
        int lastValue;    //needed to prevent TextBox from overwriting external changes to value

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
            labelRect.xMax -= area.width * 0.15f;
            labelRect.height = lineHeight;

            bool active = this.isActive?.Invoke() ?? true;
            float val = valGetter();
            string centerLabel = (this.labelGetter == null) ? this.label : this.labelGetter();
            Widgets.Label(labelRect, centerLabel + ": " + val);

            Rect sliderRect = new Rect(labelRect);
            sliderRect.y += lineHeight;
            sliderRect.xMin += 5;
            sliderRect.xMax -= 5;

            //Disable the slider by setting min/max to val
            int newValue = (int)Widgets.HorizontalSlider(sliderRect, val
                                               , active ? minGetter() : val
                                               , active ? maxGetter() : val
                                               , false, null, null, null, roundTo: 1f);

            area.height = 2 * lineHeight;

            Rect textEntryRect = new Rect(area);
            textEntryRect.xMin = sliderRect.xMax + 5;
            textEntryRect.yMin += lineHeight / 2;
            textEntryRect.height = lineHeight;

            int newValue2 = (int)val;
            Widgets.TextFieldNumeric<int>(textEntryRect, ref newValue2, ref editBuffer
                                , active ? minGetter() : val, active ? maxGetter() : val);

            if (newValue != (int)val) { //Value has changed from the slider
                valSetter(newValue);
                editBuffer = newValue.ToString();
            }
            if (newValue2 != (int)val) { //Value reported by TextBox different than current value
                if (newValue2 == lastValue)    //No change here, change was external
                    editBuffer = val.ToString();    //Update textbox for external change
                else                                
                    valSetter(newValue2);
            }

            lastValue = (int)valGetter();                   
            
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
                 