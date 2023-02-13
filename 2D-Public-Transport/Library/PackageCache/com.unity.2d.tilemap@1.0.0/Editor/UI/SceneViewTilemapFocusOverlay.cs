using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
    internal class SceneViewTilemapFocusOverlay : ToolbarOverlay, ITransientOverlay
    {
        internal const string k_OverlayId = "Scene View/Tilemap Focus";
        private const string k_DisplayName = "Tilemap Focus";

        public SceneViewTilemapFocusOverlay() : base(new[] {"Tile Palette/Focus Label", "Tile Palette/Focus Dropdown"})
        {}

        public bool visible =>
            GridPaintingState.defaultBrush != null && GridPaintingState.scenePaintTarget != null &&
            GridPaintPaletteWindow.isActive;
    }

    [EditorToolbarElement("Tile Palette/Focus Label")]
    sealed class TilePaletteFocusLabel : VisualElement
    {
        const string k_ToolSettingsClass = "unity-tool-settings";

        readonly TextElement m_Label;

        private static string k_LabelText = L10n.Tr("Focus On");

        public TilePaletteFocusLabel()
        {
            name = "Focus Label";
            AddToClassList(k_ToolSettingsClass);

            m_Label = new TextElement();
            m_Label.AddToClassList(EditorToolbar.elementLabelClassName);
            m_Label.text = k_LabelText;
            Add(m_Label);
        }
    }

    [EditorToolbarElement("Tile Palette/Focus Dropdown")]
    sealed class TilePaletteFocusDropdown : EditorToolbarDropdown
    {
        const string k_DropdownIconClass = "unity-toolbar-dropdown-label-icon";
        const string k_ToolSettingsClass = "unity-tool-settings";

        readonly TextElement m_Label;
        readonly VisualElement m_Icon;

        readonly GUIContent m_None;
        readonly GUIContent m_Tilemap;
        readonly GUIContent m_Grid;

        public TilePaletteFocusDropdown()
        {
            name = "Focus Dropdown";
            AddToClassList(k_ToolSettingsClass);

            m_None = EditorGUIUtility.TrTextContentWithIcon("None",
                "Focus Mode is not active.",
                "ToolHandlePivot");
            m_Tilemap = EditorGUIUtility.TrTextContentWithIcon("Tilemap",
                "Focuses on the active Tilemap. Filters out all other Renderers.",
                "ToolHandlePivot");
            m_Grid = EditorGUIUtility.TrTextContentWithIcon("Grid",
                "Focuses on all Renderers with the active Grid. Filters out all other Renderers.",
                "ToolHandlePivot");

            clicked += OpenContextMenu;

            FocusModeChanged();
        }

        void OpenContextMenu()
        {
            var menu = new GenericMenu();
            var focusMode = GridPaintPaletteWindow.focusMode;
            menu.AddItem(m_None, focusMode == GridPaintPaletteWindow.TilemapFocusMode.None, () => SetFocusMode(GridPaintPaletteWindow.TilemapFocusMode.None));
            menu.AddItem(m_Tilemap, focusMode == GridPaintPaletteWindow.TilemapFocusMode.Tilemap, () => SetFocusMode(GridPaintPaletteWindow.TilemapFocusMode.Tilemap));
            menu.AddItem(m_Grid, focusMode == GridPaintPaletteWindow.TilemapFocusMode.Grid, () => SetFocusMode(GridPaintPaletteWindow.TilemapFocusMode.Grid));
            menu.DropDown(worldBound);
        }

        void SetFocusMode(GridPaintPaletteWindow.TilemapFocusMode mode)
        {
            GridPaintPaletteWindow.instances[0].SetFocusMode(mode);
            FocusModeChanged();
        }

        void FocusModeChanged()
        {
            var content = m_None;
            switch (GridPaintPaletteWindow.focusMode)
            {
                case GridPaintPaletteWindow.TilemapFocusMode.Tilemap:
                    content = m_Tilemap;
                    break;
                case GridPaintPaletteWindow.TilemapFocusMode.Grid:
                    content = m_Grid;
                    break;
            }
            text = content.text;
            tooltip = content.tooltip;
            icon = EditorGUIUtility.LoadIconRequired("TilemapRenderer Icon");
        }
    }
}
