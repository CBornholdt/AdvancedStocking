using System;
using Verse;
using UnityEngine;

namespace AdvancedStocking
{
	public class Listing_TreeUIOption : Listing_Tree 
	{
		public TreeNode_UIOption rootOption = null;
		private GameFont font;
		private Vector2 scrollPosition;

		public Listing_TreeUIOption(TreeNode_UIOption rootOption, GameFont font = GameFont.Medium, float lineHeight = 30)
		{
			this.rootOption = rootOption;
			this.font = font;
			this.lineHeight = lineHeight;
			nestIndentWidth = lineHeight;
		}

		public override void Begin(Rect rect)
		{
			Rect viewRect = new Rect (0, 0, rect.width - 16, CurHeight + 60);
			Widgets.BeginScrollView (rect, ref this.scrollPosition, viewRect, true);
			Rect rect2 = new Rect (0, 0, viewRect.width, 9999);
			base.Begin (rect2);
		}

		public void DrawUIOptions(TreeNode_UIOption node = null, int indentLevel = 0, int openMask = 1)
		{
			if (node == null && indentLevel == 0) {
				node = rootOption;
				node.SetOpen (openMask, true);
			}
			if (node == null)
				return;
				
			if (node.forcedOpen)
				node.SetOpen (openMask, true);

			Text.Font = this.font;
			Rect rect = RemainingAreaIndented (indentLevel);
			if (node.children?.Count > 0) {
				base.OpenCloseWidget (node, indentLevel, openMask);
				rect.xMin += 18;
			}

			this.curY = node.Draw (rect, lineHeight);

			if (node.IsOpen(openMask))
				foreach(TreeNode child in node.children)
					DrawUIOptions((child as TreeNode_UIOption), indentLevel + 1);
		}

		public override void End()
		{
			base.End();
			Widgets.EndScrollView();
		}

		protected Rect RemainingAreaIndented(int indentLevel = 0)
		{
			Rect r = new Rect (this.curX, this.curY, 
				this.ColumnWidth - this.curX, this.listingRect.height - this.curY);
			r.xMin += indentLevel * nestIndentWidth;
			return r;
		}
	}
}