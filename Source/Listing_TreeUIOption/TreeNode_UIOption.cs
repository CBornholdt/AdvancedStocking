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
		public string label;
		public bool forcedOpen = false;
		public Func<bool> isActive = null;
		public string tipText = string.Empty;

		public TreeNode_UIOption(string label, string tipText = null, bool forcedOpen = false, Func<bool> isActive = null) : base()
		{
			this.label = label;
			this.tipText = tipText;
			this.forcedOpen = forcedOpen;
			this.isActive = isActive;
			children = new List<TreeNode>();
		}

		public override string ToString () {
			return label;
		}

        public void Remove()
        {
            Listing_TreeUIOption.optionsToRemove.Add(this);
        }

		public virtual float Draw(Rect area, float lineHeight)
		{
			int textHeight = (int)Text.CalcHeight (label, area.width - lineHeight);
			area.height = (textHeight % (int)lineHeight == 0) ? textHeight : textHeight + ((int)lineHeight - textHeight % (int)lineHeight);

			Widgets.DrawHighlightIfMouseover (area);

			if (!this.tipText.NullOrEmpty ()) {
				if (Mouse.IsOver (area)) {
					GUI.DrawTexture (area, TexUI.HighlightTex);
				}
				TooltipHandler.TipRegion (area, this.tipText);
			}

			Widgets.Label (area, this.label);
			return area.yMax;
		}
	}
}
