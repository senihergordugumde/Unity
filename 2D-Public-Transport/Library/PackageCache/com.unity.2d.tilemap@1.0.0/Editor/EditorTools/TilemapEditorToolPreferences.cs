using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.EditorTools;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Tilemaps
{
    internal class TilemapEditorToolPreferences : ScriptableSingleton<TilemapEditorToolPreferences>
    {
        private static readonly string s_LibraryFolderPath = "Library/GridBrush";
        private static readonly string s_AssetPath = FileUtil.CombinePaths(s_LibraryFolderPath, "TilemapEditorToolPreferences.asset");

        internal class TilemapEditorToolProperties
        {
            public static readonly string defaultToolsEditorPref = "TilemapEditorTool.DefaultTools";

            public static readonly GUIContent defaultToolsLabel = EditorGUIUtility.TrTextContent("Default Tile Palette Tools");
            public static readonly GUIContent addLabel = EditorGUIUtility.TrTextContent("+", "Add to Defaults");
            public static readonly GUIContent removeLabel = EditorGUIUtility.TrTextContent("-", "Remove from Defaults");
            public static readonly GUIContent saveLabel = EditorGUIUtility.TrTextContent("Save", "Save Defaults");
            public static readonly GUIContent revertLabel = EditorGUIUtility.TrTextContent("Revert", "Revert Changes");
            public static readonly GUIContent resetLabel = EditorGUIUtility.TrTextContent("Reset", "Reset Defaults");
        }

        internal static void PreferencesGUI()
        {
            using (new SettingsWindow.GUIScope())
            {
                instance.DoPreferencesGUI();
            }
        }

        internal static EditorTool[] CreateDefaultTilePaletteEditorTools()
        {
            return instance.CreateDefaultTilemapEditorTools();
        }

        private ReorderableList m_DefaultTypes;
        private ReorderableList m_OtherTypes;
        private bool m_Changed;

        private HashSet<Type> m_AllTilemapEditorToolTypes;
        private List<DefaultTilemapEditorTool> m_DefaultTilemapEditorToolTypes;
        private List<DefaultTilemapEditorTool> m_OtherTilemapEditorToolTypes;

        private static EditorTool[] s_DefaultTilemapEditorTools;

        private void OnEnable()
        {
            m_DefaultTilemapEditorToolTypes = new List<DefaultTilemapEditorTool>();
            m_OtherTilemapEditorToolTypes = new List<DefaultTilemapEditorTool>();

            LoadDefaultEditorToolTypes();

            m_DefaultTypes = new ReorderableList(m_DefaultTilemapEditorToolTypes, typeof(DefaultTilemapEditorTool), true, false, false, false);
            m_DefaultTypes.drawElementCallback = OnDrawDefaultElement;
            m_DefaultTypes.onReorderCallback = OnReorderDefaultElement;

            m_OtherTypes = new ReorderableList(m_OtherTilemapEditorToolTypes, typeof(DefaultTilemapEditorTool), true, false, false, false);
            m_OtherTypes.drawElementCallback = OnDrawOtherElement;

            m_Changed = false;
        }

        private void InitializeAllTilemapEditorToolTypes()
        {
            m_AllTilemapEditorToolTypes = new HashSet<Type>();
            foreach (var tilemapEditorToolType in TypeCache.GetTypesDerivedFrom(typeof(TilemapEditorTool)))
            {
                if (tilemapEditorToolType.IsAbstract)
                    continue;
                m_AllTilemapEditorToolTypes.Add(tilemapEditorToolType);
            }
        }

        internal void LoadDefaultEditorToolTypes()
        {
            InitializeAllTilemapEditorToolTypes();

            m_DefaultTilemapEditorToolTypes.Clear();
            m_OtherTilemapEditorToolTypes.Clear();

            var defaultTools = LoadTilemapEditorToolPreferencesAsset();
            if (defaultTools == null || defaultTools.Count == 0)
            {
                foreach (var type in TilemapEditorTool.s_DefaultToolTypes)
                {
                    m_DefaultTilemapEditorToolTypes.Add(new DefaultTilemapEditorTool { fullTypeName = type.FullName, tilemapEditorToolType = type });
                }
            }
            else
            {
                foreach (var defaultTool in defaultTools)
                {
                    if (!String.IsNullOrWhiteSpace(defaultTool.fullTypeName))
                    {
                        Type toolType = null;
                        foreach (var type in m_AllTilemapEditorToolTypes)
                        {
                            if (type.FullName == defaultTool.fullTypeName)
                            {
                                toolType = type;
                                break;
                            }
                        }
                        m_DefaultTilemapEditorToolTypes.Add(new DefaultTilemapEditorTool { fullTypeName = defaultTool.fullTypeName, tilemapEditorToolType = toolType });
                    }
                }
            }

            HashSet<Type> otherTypes = new HashSet<Type>(m_AllTilemapEditorToolTypes);
            foreach (var defaultType in m_DefaultTilemapEditorToolTypes)
            {
                if (defaultType.tilemapEditorToolType != null)
                    otherTypes.Remove(defaultType.tilemapEditorToolType);
            }

            foreach (var otherType in otherTypes)
            {
                m_OtherTilemapEditorToolTypes.Add(new DefaultTilemapEditorTool
                    {fullTypeName = otherType.FullName, tilemapEditorToolType = otherType});
            }
        }

        private EditorTool[] CreateDefaultTilemapEditorTools()
        {
            if (m_DefaultTilemapEditorToolTypes == null)
                LoadDefaultEditorToolTypes();

            // Create Tools based on the user's preferences
            List<EditorTool> editorTools = new List<EditorTool>();
            foreach (var defaultTool in m_DefaultTilemapEditorToolTypes)
            {
                if (defaultTool.tilemapEditorToolType != null)
                {
                    var tool = ScriptableObject.CreateInstance(defaultTool.tilemapEditorToolType) as EditorTool;
                    if (tool != null)
                        editorTools.Add(tool);
                }
            }

            // Ensure that there are always tools for the Tile Palette
            if (editorTools.Count == 0)
            {
                foreach (var defaultToolType in TilemapEditorTool.s_DefaultToolTypes)
                {
                    editorTools.Add(ScriptableObject.CreateInstance(defaultToolType) as EditorTool);
                }
            }

            s_DefaultTilemapEditorTools = editorTools.ToArray();
            return s_DefaultTilemapEditorTools;
        }

        internal void ClearExistingDefaultTilemapEditorTools()
        {
            if (s_DefaultTilemapEditorTools == null)
                return;

            for (int i = 0; i < s_DefaultTilemapEditorTools.Length; ++i)
            {
                s_DefaultTilemapEditorTools[i] = null;
            }
        }

        private void OnDrawDefaultElement(Rect rect, int i, bool isactive, bool isfocused)
        {
            if (i < 0 || i >= m_DefaultTilemapEditorToolTypes.Count)
                return;

            var type = m_DefaultTilemapEditorToolTypes[i];
            DoDrawElement(rect, type);
        }

        private void OnDrawOtherElement(Rect rect, int i, bool isactive, bool isfocused)
        {
            if (i < 0 || i >= m_OtherTilemapEditorToolTypes.Count)
                return;

            var type = m_OtherTilemapEditorToolTypes[i];
            DoDrawElement(rect, type);
        }

        private void DoDrawElement(Rect rect, DefaultTilemapEditorTool defaultType)
        {
            if (defaultType.tilemapEditorToolType == null)
            {
                EditorGUI.LabelField(rect, GUIContent.Temp(String.Format("[{0}] {1}", "Invalid", defaultType.fullTypeName)));
            }
            else
            {
                if (defaultType.toolInstance == null)
                    defaultType.toolInstance = (TilemapEditorTool)ScriptableObject.CreateInstance(defaultType.tilemapEditorToolType);
                if (defaultType.toolInstance != null && defaultType.toolInstance.toolbarIcon != null)
                {
                    EditorGUI.LabelField(rect, defaultType.toolInstance.toolbarIcon);
                    var size = GUI.skin.label.CalcSize(defaultType.toolInstance.toolbarIcon);
                    rect.xMin += size.x;
                }
                EditorGUI.LabelField(rect, GUIContent.Temp(defaultType.tilemapEditorToolType.Name, defaultType.tilemapEditorToolType.FullName));
            }
        }

        private void OnReorderDefaultElement(ReorderableList list)
        {
            m_Changed = true;
        }

        private void DoPreferencesGUI()
        {
            if (m_DefaultTypes == null)
                LoadDefaultEditorToolTypes();

            EditorGUILayout.LabelField(TilemapEditorToolProperties.defaultToolsLabel);

            EditorGUILayout.BeginHorizontal();

            m_DefaultTypes.DoLayoutList();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(TilemapEditorToolProperties.addLabel))
            {
                var otherIndex = m_OtherTypes.index;
                if (0 <= otherIndex && otherIndex < m_OtherTilemapEditorToolTypes.Count)
                {
                    var otherType = m_OtherTilemapEditorToolTypes[otherIndex];
                    var defaultIndex = m_DefaultTypes.index;
                    m_DefaultTilemapEditorToolTypes.Insert(defaultIndex + 1, otherType);
                    m_OtherTilemapEditorToolTypes.RemoveAt(otherIndex);

                    m_DefaultTypes.index = defaultIndex + 1;
                    m_OtherTypes.index = -1;

                    m_Changed = true;
                }
            }
            if (GUILayout.Button(TilemapEditorToolProperties.removeLabel))
            {
                var defaultIndex = m_DefaultTypes.index;
                if (0 <= defaultIndex && defaultIndex < m_DefaultTilemapEditorToolTypes.Count)
                {
                    var defaultType = m_DefaultTilemapEditorToolTypes[defaultIndex];
                    if (defaultType.tilemapEditorToolType != null)
                    {
                        var otherIndex = m_OtherTypes.index;
                        m_OtherTilemapEditorToolTypes.Insert(otherIndex + 1, defaultType);
                        m_OtherTypes.index = otherIndex + 1;
                    }
                    m_DefaultTilemapEditorToolTypes.RemoveAt(defaultIndex);
                    m_DefaultTypes.index = -1;

                    m_Changed = true;
                }
            }

            EditorGUILayout.EndVertical();

            m_OtherTypes.DoLayoutList();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!m_Changed))
            {
                if (GUILayout.Button(TilemapEditorToolProperties.saveLabel))
                {
                    SaveTilemapEditorToolPreferencesAsset(m_DefaultTilemapEditorToolTypes);
                    ClearExistingDefaultTilemapEditorTools();
                    m_Changed = false;
                    GridPaintingState.RepaintGridPaintPaletteWindow();
                }
                if (GUILayout.Button(TilemapEditorToolProperties.revertLabel))
                {
                    LoadDefaultEditorToolTypes();
                    m_DefaultTypes.index = -1;
                    m_OtherTypes.index = -1;
                    m_Changed = false;
                }
            }
            if (GUILayout.Button(TilemapEditorToolProperties.resetLabel))
            {
                DeleteTilemapEditorToolPreferencesAsset();
                ClearExistingDefaultTilemapEditorTools();
                LoadDefaultEditorToolTypes();
                m_DefaultTypes.index = -1;
                m_OtherTypes.index = -1;
                m_Changed = false;
                GridPaintingState.RepaintGridPaintPaletteWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        internal void DeleteTilemapEditorToolPreferencesAsset()
        {
            if (File.Exists(s_AssetPath))
            {
                FileUtil.DeleteFileOrDirectory(s_AssetPath);
            }
        }

        internal void SaveTilemapEditorToolPreferencesAsset(List<DefaultTilemapEditorTool> defaultTools)
        {
            if (defaultTools == null)
                return;

            foreach (var defaultTool in defaultTools)
            {
                if (defaultTool.tilemapEditorToolType != null &&
                    !(defaultTool.tilemapEditorToolType.IsSubclassOf(typeof(TilemapEditorTool))))
                {
                    return;
                }
            }

            if (!Directory.Exists(s_LibraryFolderPath))
            {
                Directory.CreateDirectory(s_LibraryFolderPath);
            }

            var saveAsset = ScriptableObject.CreateInstance<TilemapEditorToolPreferencesAsset>();
            saveAsset.m_UserDefaultTools = defaultTools;
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { saveAsset }, s_AssetPath, true);
        }

        internal List<DefaultTilemapEditorTool> LoadTilemapEditorToolPreferencesAsset()
        {
            if (!File.Exists(s_AssetPath))
                return null;

            var serializedObjects = InternalEditorUtility.LoadSerializedFileAndForget(s_AssetPath);
            if (serializedObjects != null && serializedObjects.Length > 0)
            {
                var defaultTools = serializedObjects[0] as TilemapEditorToolPreferencesAsset;
                if (defaultTools != null)
                    return defaultTools.m_UserDefaultTools;
            }
            return null;
        }
    }
}
