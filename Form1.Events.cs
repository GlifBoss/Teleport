// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Teleport
{
    public partial class Form1
    {
        private void customThumb_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int delta = e.Y - dragStartPoint.Y;
                int newTop = customThumb.Top + delta;
                newTop = Math.Max(0, Math.Min(newTop, scrollBarCover.Height - customThumb.Height));
                customThumb.Top = newTop;
                var allVisibleNodes = GetAllVisibleNodes(treeView.Nodes);
                if (allVisibleNodes.Count > treeView.VisibleCount)
                {
                float ratio = (float)newTop / (float)(scrollBarCover.Height - customThumb.Height);
                int index = (int)(ratio * (allVisibleNodes.Count - treeView.VisibleCount));
                if (index >= 0 && index < allVisibleNodes.Count)
                treeView.TopNode = allVisibleNodes[index];
                }
            }
        }

        private void TreeView_BeforeCollapse(object? sender, TreeViewCancelEventArgs e)
        {
        if (!allowExpandCollapse)
        e.Cancel = true;
        }

        private void TreeView_MouseDown(object? sender, MouseEventArgs e)
        {
        TreeViewHitTestInfo hit = treeView.HitTest(e.Location);
        allowExpandCollapse = hit.Location == TreeViewHitTestLocations.PlusMinus;
        }

        private void TreeView_AfterExpand(object? sender, TreeViewEventArgs e)
        {
            TreeNode? expandedNode = e.Node;
            if (expandedNode == null) return;
            TreeNodeCollection nodes = expandedNode.Parent == null ? treeView.Nodes : expandedNode.Parent.Nodes;
            foreach (TreeNode node in nodes)
            {
                if (node != expandedNode)
                {
                allowExpandCollapse = true;
                node.Collapse();
                allowExpandCollapse = false;
                }
            }
        }

        private void TreeView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (treeView.SelectedNode != null)
                {
                TreeView_NodeMouseDoubleClick(treeView, new TreeNodeMouseClickEventArgs(treeView.SelectedNode, MouseButtons.Left, 2, 0, 0));
                }
                e.SuppressKeyPress = true;
            }
        }

        private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (previousNode != null)
            {
            previousNode.BackColor = treeView.BackColor;
            previousNode.ForeColor = treeView.ForeColor;
            }
            if (e.Node != null)
            {
            e.Node.BackColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
            e.Node.ForeColor = treeView.ForeColor;
            previousNode = e.Node;
            }
        }

        private void TreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;
            bool isSelected = (e.State & TreeNodeStates.Selected) != 0;
            Color backColor = isSelected ? (ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255)) : ThemeManager.BackColor;
            Color foreColor = isSelected ? (ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(100, 180, 255) : Color.FromArgb(0, 0, 255)) : ThemeManager.TextColor;
            using (SolidBrush backBrush = new SolidBrush(backColor))
            e.Graphics.FillRectangle(backBrush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, treeView.Font, e.Bounds, foreColor, TextFormatFlags.VerticalCenter);
        }
    }
}