using UnityEditor.Overlays;
using UnityEditor.Toolbars;

namespace UnityEditor.Tilemaps
{
    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
    internal class SceneViewOpenTilePaletteOverlay : ToolbarOverlay, ITransientOverlay
    {
        internal const string k_OverlayId = "Scene View/Open Tile Palette";
        private const string k_DisplayName = "Open Tile Palette";

        public SceneViewOpenTilePaletteOverlay() : base("Tile Palette/Open Palette") {}

        public bool visible => SceneViewOpenTilePaletteHelper.showInSceneViewActive && SceneViewOpenTilePaletteHelper.IsActive();
    }

    [EditorToolbarElement("Tile Palette/Open Palette")]
    sealed class TilePaletteOpenPalette : EditorToolbarButton
    {
        const string k_ToolSettingsClass = "unity-tool-settings";

        private static string k_LabelText = L10n.Tr("Open Tile Palette");
        private static string k_TooltipText = L10n.Tr("Opens the Tile Palette Window");

        public TilePaletteOpenPalette() : base(SceneViewOpenTilePaletteHelper.OpenTilePalette)
        {
            name = "Open Tile Palette";
            AddToClassList(k_ToolSettingsClass);

            icon = EditorGUIUtility.LoadIconRequired("Tilemap Icon");
            text = k_LabelText;
            tooltip = k_TooltipText;
        }
    }
}
