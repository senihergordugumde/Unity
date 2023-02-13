using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Tool for doing a rotate action with the Tile Palette
    /// </summary>
    public abstract class RotateTool : TilemapEditorTool
    {
        /// <summary>
        /// Handles rotation in the given direction when the RotateTool is activated
        /// </summary>
        /// <param name="direction">Direction to rotate by</param>
        protected void Rotate(GridBrushBase.RotationDirection direction)
        {
            if (GridPaintingState.gridBrush == null)
                return;

            var grid = GridPaintingState.activeGrid;
            if (grid == null)
                grid = GridPaintingState.lastActiveGrid;
            if (grid != null && grid.isActive)
            {
                GridPaintingState.gridBrush.Rotate(direction, grid.cellLayout);
                grid.Repaint();
            }
            else if (GridPaintingState.scenePaintTarget != null)
            {
                var gridLayout = GridPaintingState.scenePaintTarget.GetComponentInParent<GridLayout>();
                if (gridLayout != null)
                {
                    GridPaintingState.gridBrush.Rotate(direction, gridLayout.cellLayout);
                }
            }
        }

        /// <summary>
        /// Handles GUI for the RotateTool when the Tool is active
        /// </summary>
        /// <param name="window">EditorWindow from which OnToolGUI is called.</param>
        public override void OnToolGUI(EditorWindow window)
        {
            ToolManager.RestorePreviousTool();
        }
    }

    /// <summary>
    /// Tool for doing a rotate clockwise action with the Tile Palette
    /// </summary>
    public sealed class RotateClockwiseTool : RotateTool
    {
        private static class Styles
        {
            public static string tooltipStringFormat = L10n.Tr("|Rotates the contents of the brush clockwise. ({0})");
            public static string shortcutId = "Grid Painting/Rotate Clockwise";
            public static GUIContent toolContent = EditorGUIUtility.IconContent("Packages/com.unity.2d.tilemap/Editor/Icons/Grid.RotateCW.png", GetTooltipText(tooltipStringFormat, shortcutId));
        }

        /// <summary>
        /// Tooltip String Format for the RotateClockwiseTool
        /// </summary>
        protected override string tooltipStringFormat
        {
            get { return Styles.tooltipStringFormat; }
        }

        /// <summary>
        /// Shortcut Id for the RotateClockwiseTool
        /// </summary>
        protected override string shortcutId
        {
            get { return Styles.shortcutId; }
        }

        /// <summary>
        /// Toolbar Icon for the RotateClockwiseTool
        /// </summary>
        public override GUIContent toolbarIcon
        {
            get { return Styles.toolContent; }
        }

        /// <summary>
        /// Action when RotateClockwiseTool is activated
        /// </summary>
        public override void OnActivated()
        {
            Rotate(GridBrushBase.RotationDirection.Clockwise);
        }
    }

    /// <summary>
    /// Tool for doing a rotate counter clockwise action with the Tile Palette
    /// </summary>
    public sealed class RotateCounterClockwiseTool : RotateTool
    {
        private static class Styles
        {
            public static string tooltipStringFormat = L10n.Tr("|Rotates the contents of the brush counter clockwise. ({0})");
            public static string shortcutId = "Grid Painting/Rotate Anti-Clockwise";
            public static GUIContent toolContent = EditorGUIUtility.IconContent("Packages/com.unity.2d.tilemap/Editor/Icons/Grid.RotateACW.png", GetTooltipText(tooltipStringFormat, shortcutId));
        }

        /// <summary>
        /// Tooltip String Format for the RotateCounterClockwiseTool
        /// </summary>
        protected override string tooltipStringFormat
        {
            get { return Styles.tooltipStringFormat; }
        }

        /// <summary>
        /// Shortcut Id for the RotateCounterClockwiseTool
        /// </summary>
        protected override string shortcutId
        {
            get { return Styles.shortcutId; }
        }

        /// <summary>
        /// Toolbar Icon for the RotateCounterClockwiseTool
        /// </summary>
        public override GUIContent toolbarIcon
        {
            get { return Styles.toolContent; }
        }

        /// <summary>
        /// Action when RotateCounterClockwiseTool is activated
        /// </summary>
        public override void OnActivated()
        {
            Rotate(GridBrushBase.RotationDirection.CounterClockwise);
        }
    }
}
