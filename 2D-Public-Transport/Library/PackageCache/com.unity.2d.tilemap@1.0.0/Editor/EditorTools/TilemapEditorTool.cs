using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// A base class for Editor Tools which work with the Tile Palette
    /// and GridBrushes
    /// </summary>
    public abstract class TilemapEditorTool : EditorTool
    {
        /// <summary>
        /// Context to determine if TilemapEditorTools can be triggered through shortcuts
        /// </summary>
        public class ShortcutContext : IShortcutToolContext
        {
            public bool active { get; set; }
        }

        private static Dictionary<Type, EditorTool> s_TilemapEditorToolsMap;
        private static EditorTool[] s_DefaultTilemapEditorTools;

        /// <summary>
        /// Types for all the Default Editor Tools
        /// </summary>
        internal static Type[] s_DefaultToolTypes = new[]
        {
            typeof(SelectTool),
            typeof(MoveTool),
            typeof(PaintTool),
            typeof(BoxTool),
            typeof(PickingTool),
            typeof(EraseTool),
            typeof(FillTool)
        };

        /// <summary>
        /// All currently active Editor Tools which work with the Tile Palette
        /// </summary>
        public static EditorTool[] tilemapEditorTools
        {
            get
            {
                if (IsCachedEditorToolsInvalid())
                    InstantiateEditorTools();
                return GridPaintingState.activeBrushTools;
            }
        }

        /// <summary>
        /// The horizontal size of a Toolbar with all the TilemapEditorTools
        /// </summary>
        public static float tilemapEditorToolsToolbarSize
        {
            get
            {
                if (IsCachedEditorToolsInvalid())
                    InstantiateEditorTools();
                return GridPaintingState.activeBrushToolbarSize;
            }
        }

        /// <summary>
        /// Tooltip String format which accepts a shortcut combination as the parameter
        /// </summary>
        protected abstract string tooltipStringFormat { get; }
        /// <summary>
        /// Shortcut Id for this tool
        /// </summary>
        protected abstract string shortcutId { get; }

        /// <summary>
        /// Gets the text for the tooltip given a tooltip string format
        /// and the shortcut combination for a tooltip
        /// </summary>
        /// <param name="tooltipStringFormat">String format which accepts a shortcut combination as the parameter</param>
        /// <param name="shortcutId">Shortcut Id for this tool</param>
        /// <returns>The final text for the tooltip</returns>
        protected static string GetTooltipText(string tooltipStringFormat, string shortcutId)
        {
            return String.Format(tooltipStringFormat, GetKeysFromToolName(shortcutId));
        }

        /// <summary>
        /// Gets the key combination for triggering the shortcut for this tool
        /// </summary>
        /// <param name="id">The shortcut id for this tool</param>
        /// <returns>The key combination for triggering the shortcut for this tool</returns>
        protected static string GetKeysFromToolName(string id)
        {
            return ShortcutIntegration.instance.GetKeyCombinationFor(id);
        }

        /// <summary>
        /// Updates the tooltip whenever there is a change in shortcut combinations
        /// </summary>
        protected void UpdateTooltip()
        {
            toolbarIcon.tooltip = GetTooltipText(tooltipStringFormat, shortcutId);
        }

        /// <summary>
        /// Method called when the Tool is being used.
        /// Override this to have custom behaviour when the Tool is used.
        /// </summary>
        /// <param name="isHotControl">Whether the tool is the hot control</param>
        /// <param name="gridLayout">GridLayout the tool is being used on</param>
        /// <param name="brushTarget">Target GameObject the tool is being used on</param>
        /// <param name="gridMousePosition">Grid Cell position of the Mouse on the GridLayout</param>
        /// <returns>Whether the tool has been used and modified the brushTarget</returns>
        public virtual bool HandleTool(bool isHotControl, GridLayout gridLayout, GameObject brushTarget, Vector3Int gridMousePosition)
        {
            return false;
        }

        /// <summary>
        /// Gets whether the tool is available for use
        /// </summary>
        /// <returns>Whether the tool is available for use</returns>
        public override bool IsAvailable()
        {
            return (GridPaintPaletteWindow.instances.Count > 0) && GridPaintingState.gridBrush;
        }

        internal static void UpdateTooltips()
        {
            if (IsCachedEditorToolsInvalid())
                InstantiateEditorTools();

            foreach (var editorTool in GridPaintingState.activeBrushTools)
            {
                var tilemapEditorTool = editorTool as TilemapEditorTool;
                if (tilemapEditorTool == null)
                    return;

                tilemapEditorTool.UpdateTooltip();
            }
        }

        /// <summary>
        /// Toggles the state of active editor tool with the type passed in.
        /// </summary>
        /// <remarks>
        /// This will change the current active editor tool if the type passed in
        /// is not the same as the current active editor tool. Otherwise, it will
        /// set the View Mode tool as the current active editor tool.
        /// </remarks>
        /// <param name="type">
        /// The type of editor tool. This must be inherited from EditorTool.
        /// </param>
        public static void ToggleActiveEditorTool(Type type)
        {
            if (ToolManager.activeToolType != type)
            {
                SetActiveEditorTool(type);
            }
            else
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }

        /// <summary>
        /// Sets the current active editor tool to the type passed in
        /// </summary>
        /// <param name="type">The type of editor tool. This must be inherited from TilemapEditorTool</param>
        /// <exception cref="ArgumentException">Throws this if an invalid type parameter is set</exception>
        public static void SetActiveEditorTool(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(TilemapEditorTool)))
                throw new ArgumentException("The tool to set must be valid and derive from TilemapEditorTool.");

            EditorTool selectedTool = null;
            foreach (var tool in tilemapEditorTools)
            {
                if (tool.GetType() == type)
                {
                    selectedTool = tool;
                    break;
                }
            }

            if (selectedTool != null)
            {
                ToolManager.SetActiveTool(selectedTool);
            }
        }

        internal static bool IsActive(Type toolType)
        {
            return ToolManager.activeToolType != null && ToolManager.activeToolType == toolType;
        }

        private static bool IsCachedEditorToolsInvalid()
        {
            return s_TilemapEditorToolsMap == null
                || s_DefaultTilemapEditorTools == null
                || s_DefaultTilemapEditorTools.Length == 0
                || s_DefaultTilemapEditorTools[0] == null;
        }

        private static void InstantiateEditorTools()
        {
            s_DefaultTilemapEditorTools = TilemapEditorToolPreferences.CreateDefaultTilePaletteEditorTools();
            s_TilemapEditorToolsMap = new Dictionary<Type, EditorTool>(s_DefaultTilemapEditorTools.Length);
            foreach (var editorTool in s_DefaultTilemapEditorTools)
            {
                s_TilemapEditorToolsMap.Add(editorTool.GetType(), editorTool);
            }
            GridPaintingState.UpdateBrushToolbar();
        }

        internal static void UpdateEditorTools(BrushToolsAttribute brushToolsAttribute)
        {
            if (IsCachedEditorToolsInvalid())
                InstantiateEditorTools();
            EditorTool[] editorTools;
            if (brushToolsAttribute?.toolList == null || brushToolsAttribute.toolList.Count == 0)
            {
                editorTools = s_DefaultTilemapEditorTools;
            }
            else
            {
                editorTools = new EditorTool[brushToolsAttribute.toolList.Count];
                for (int i = 0; i < brushToolsAttribute.toolList.Count; ++i)
                {
                    var toolType = brushToolsAttribute.toolList[i];
                    if (!s_TilemapEditorToolsMap.TryGetValue(toolType, out EditorTool editorTool))
                    {
                        editorTool = (EditorTool)ScriptableObject.CreateInstance(toolType);
                        s_TilemapEditorToolsMap.Add(toolType, editorTool);
                    }
                    editorTools[i] = editorTool;
                }
            }
            GridPaintingState.SetBrushTools(editorTools);
        }

        internal static bool IsCustomTilemapEditorToolActive()
        {
            if (EditorToolManager.activeTool == null
                || !(EditorToolManager.activeTool is TilemapEditorTool))
                return false;

            if (s_DefaultTilemapEditorTools == null)
                return false;

            var activeToolType = EditorToolManager.activeTool.GetType();
            foreach (var toolType in s_DefaultToolTypes)
            {
                if (toolType == activeToolType)
                    return false;
            }

            return true;
        }
    }
}
