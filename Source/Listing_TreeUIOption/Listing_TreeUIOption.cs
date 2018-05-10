using System;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace AdvancedStocking
{
	public class Listing_TreeUIOption : Listing_Tree 
	{
		private List<TreeNode_UIOption> rootOptions = null;
		private GameFont font;
		private Vector2 scrollPosition;
		private readonly Vector2 closeButtonSize = new Vector2(16, 16);

		public Listing_TreeUIOption(List<TreeNode_UIOption> rootOptions, GameFont font = GameFont.Medium, float lineHeight = 30 )
		{
			this.rootOptions = rootOptions;
			this.font = font;
			this.lineHeight = lineHeight;
			nestIndentWidth = lineHeight * 0.75f;
		}

		public List<TreeNode_UIOption> RootOptions {
			get { return this.rootOptions; }
		}

		public override void Begin(Rect rect)
		{
			Rect viewRect = new Rect (0, 0, rect.width - closeButtonSize.x, CurHeight + 60);	// + 60 is there for padding
			Widgets.BeginScrollView (rect, ref this.scrollPosition, viewRect, true);
			Rect rect2 = new Rect (0, 0, viewRect.width, 9999);
			base.Begin (rect2);
		}

		public void DrawUIOptions(TreeNode_UIOption node = null, int indentLevel = 0, int openMask = 1)
		{
			if (node == null && indentLevel == 0) {
				foreach (var option in this.rootOptions)
					DrawUIOptions (option, 0, openMask);
			//	node.SetOpen (openMask, true);
			}
			if (node == null)
				return;
				
			if (node.forcedOpen)
				node.SetOpen (openMask, true);

			Text.Font = this.font;
			Rect rect = RemainingAreaIndented (indentLevel);
			if (node.children?.Count > 0) {
				base.OpenCloseWidget (node, indentLevel, openMask);
				rect.xMin += OpenCloseWidgetSize;
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
			Rect r = new Rect (0, this.curY, 
				this.listingRect.width, this.listingRect.height - this.curY);
			r.xMin += indentLevel * nestIndentWidth;
			return r;
		}
	}
}