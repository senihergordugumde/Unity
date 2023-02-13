using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Tool for doing a flip action with the Tile Palette
    /// </summary>
    public abstract class FlipTool : TilemapEditorTool
    {
        /// <summary>
        /// Handles flipping in the given direction when the FlipTool is activated
        /// </summary>
        /// <param name="axis">Axis to flip by</param>
        protected void Flip(GridBrushBase.FlipAxis axis)
        {
            if (GridPaintingState.gridBrush == null)
                return;

            var grid = GridPaintingState.activeGrid;
            if (grid == null)
                grid = GridPaintingState.lastActiveGrid;
            if (grid != null && grid.isActive)
            {
                GridPaintingState.gridBrush.Flip(axis, grid.cellLayout);
                grid.Repaint();
            }
            else if (GridPaintingState.scenePaintTarget != null)
            {
                var gridLayout = GridPaintingState.scenePaintTarget.GetComponentInParent<GridLayout>();
                if (gridLayout != null)
                {
                    GridPaintingState.gridBrush.Flip(axis, gridLayout.cellLayout);
                }
            }
        }

        /// <summary>
        /// Handles GUI for the FlipTool when the Tool is active
        /// </summary>
        /// <param name="window">EditorWindow from which OnToolGUI is called.</param>
        public override void OnToolGUI(EditorWindow window)
        {
            ToolManager.RestorePreviousTool();
        }
    }

    /// <summary>
    /// Tool for doing a flip X action with the Tile Palette
    /// </summary>
    public sealed class FlipXTool : FlipTool
    {
        private static class Styles
        {
            public static string tooltipStringFormat = L10n.Tr("|Flips the contents of the brush in the X Axis. ({0})");
            public static string shortcutId = "Grid Painting/Flip X";
            public static GUIContent toolContent = EditorGUIUtility.IconContent("Packages/com.unity.2d.tilemap/Editor/Icons/Grid.FlipX.png", GetTooltipText(tooltipStringFormat, shortcutId));
        }

        /// <summary>
        /// Tooltip String Format for the FlipXTool
        /// </summary>
        protected override string tooltipStringFormat
        {
            get { return Styles.tooltipStringFormat; }
        }

        /// <summary>
        /// Shortcut Id for the FlipXTool
        /// </summary>
        protected override string shortcutId
        {
            get { return Styles.shortcutId; }
        }

        /// <summary>
        /// Toolbar Icon for the FlipXTool
        /// </summary>
        public override GUIContent toolbarIcon
        {
            get { return Styles.toolContent; }
        }

        /// <summary>
        /// Action when FlipXTool is activated
        /// </summary>
        public override void OnActivated()
        {
            Flip(GridBrushBase.FlipAxis.X);
        }
    }

    /// <summary>
    /// Tool for doing a flip Y action with the Tile Palette
    /// </summary>
    public sealed class FlipYTool : FlipTool
    {
        private static class Styles
        {
            public static string tooltipStringFormat = L10n.Tr("|Flips the contents of the brush in the Y axis. ({0})");
            public static string shortcutId = "Grid Painting/Flip Y";
            public static GUIContent toolContent = EditorGUIUtility.IconContent("Packages/com.unity.2d.tilemap/Editor/Icons/Grid.FlipY.png", GetTooltipText(tooltipStringFormat, shortcutId));
        }

        /// <summary>
        /// Tooltip String Format for the FlipYTool
        /// </summary>
        protected override string tooltipStringFormat
        {
            get { return Styles.tooltipStringFormat; }
        }

        /// <summary>
        /// Shortcut Id for the FlipYTool
        /// </summary>
        protected override string shortcutId
        {
            get { return Styles.shortcutId; }
        }

        /// <summary>
        /// Toolbar Icon for the FlipYTool
        /// </summary>
        public override GUIContent toolbarIcon
        {
            get { return Styles.toolContent; }
        }

        /// <summary>
        /// Action when FlipYTool is activated
        /// </summary>
        public override void OnActivated()
        {
            Flip(GridBrushBase.FlipAxis.Y);
        }
    }
}
