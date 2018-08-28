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

        static public Listing_TreeUIOption currentListing;
        static public List<TreeNode_UIOption> optionsToRemove = new List<TreeNode_UIOption>();

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
            currentListing = this;
			Rect viewRect = new Rect (0, 0, rect.width - closeButtonSize.x, CurHeight);	
			Widgets.BeginScrollView (rect, ref this.scrollPosition, viewRect, true);
			Rect rect2 = new Rect (0, 0, viewRect.width, CurHeight);
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
            PerformOptionRemoval(); //Performs removals requested by the options themselves AFTER drawing
            ReservationUtility.HighlightCells();
		}

        public void PerformOptionRemoval()
        {
            foreach(var option in optionsToRemove) {
                if(option.parentNode == null) {
                    rootOptions.Remove(option);
                }
                else {
                    option.parentNode.children.Remove(option);
                }
            }

            optionsToRemove.Clear();
        }

		protected Rect RemainingAreaIndented(int indentLevel = 0)
		{
			Rect r = new Rect (0, this.curY, 
				this.listingRect.width, this.listingRect.height - this.curY);
			r.xMin += indentLevel * nestIndentWidth;
			return r;
		}
	}

    public static class ListingNodeExt
    {
        public static void AddAsChildTo(this TreeNode_UIOption child, TreeNode_UIOption parent)
        {
            parent.children.Add(child);
            child.parentNode = parent;
        }
    }
}