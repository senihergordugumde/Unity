using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.U2D.PSD
{
    internal class PSDLayerImportSettingSerializedPropertyWrapper : IPSDLayerMappingStrategyComparable
    {
        PSDLayerData m_Layer;
        SerializedProperty m_Array;
        SerializedProperty m_Element;
        SerializedProperty m_NameProperty;
        SerializedProperty m_LayerIdProperty;
        SerializedProperty m_FlattenProperty;
        SerializedProperty m_IsGroup;

        public string name
        {
            get => m_NameProperty.stringValue;
            set
            {
                CheckAndAddElement();
                m_NameProperty.stringValue = value;
            }
        }

        public bool isGroup
        {
            get => m_IsGroup.boolValue;
            set
            {
                CheckAndAddElement();
                m_IsGroup.boolValue = value;
            }
        }

        public int layerID
        {
            get => m_LayerIdProperty.intValue;
            set
            {
                CheckAndAddElement();
                m_LayerIdProperty.intValue = value;
            }
        }

        public bool flatten
        {
            get => m_FlattenProperty == null ? false : m_FlattenProperty.boolValue;
            set
            {
                CheckAndAddElement();
                m_FlattenProperty.boolValue = value;
            }
        }

        void CheckAndAddElement()
        {
            if (m_Element == null)
            {
                var arraySize = m_Array.arraySize;
                m_Array.arraySize = arraySize + 1;
                m_Element = m_Array.GetArrayElementAtIndex(arraySize);
                CacheProperty(m_Element);
                flatten = false;
                name = m_Layer.name;
                layerID = m_Layer.layerID;
                isGroup = m_Layer.isGroup;
            }
        }

        void CacheProperty(SerializedProperty property)
        {
            m_NameProperty = property.FindPropertyRelative("name");
            m_LayerIdProperty = property.FindPropertyRelative("layerId");
            m_FlattenProperty = property.FindPropertyRelative("flatten");
            m_IsGroup = property.FindPropertyRelative("isGroup");

        }

        public PSDLayerImportSettingSerializedPropertyWrapper(SerializedProperty sp, SerializedProperty array, PSDLayerData layer)
        {
            if (sp != null)
            {
                m_Element = sp;
                CacheProperty(sp);
            }
            
            m_Array = array;
            m_Layer = layer;
        }
    }
    
    class PSDNode : TreeViewItem
    {
        PSDLayerData m_Layer;
        bool m_Disable = false;
        PSDLayerImportSettingSerializedPropertyWrapper m_Property;
        public bool disable
        {
            get => m_Disable;
            set => m_Disable = value;
        }
        
        public PSDNode()
        {
            id = 1;
            displayName = "";
        }
        
        public PSDNode(PSDLayerData layer, int id, PSDLayerImportSettingSerializedPropertyWrapper importSetting)
        {
            m_Layer = layer;
            displayName = layer.name;
            this.id = id;
            m_Property = importSetting;
        }

        public virtual void ChildGroupFlatten(bool flatten) { }
        public virtual void FlattenStateChange() { }
        public virtual void NotifyParentOnFlattenChange() { }
        public PSDLayerData layer => m_Layer;
        public bool flatten
        {
            get => m_Property.flatten;
            set => m_Property.flatten = value;
        }
    }

    class PSDLayerNode : PSDNode
    {
        public PSDLayerNode(PSDLayerData layer, int id, PSDLayerImportSettingSerializedPropertyWrapper property):base(layer, id, property)
        { }
    }

    class PSDLayerGroupNode : PSDNode
    {
        int m_ChildFlattenCount;
        public PSDLayerGroupNode(PSDLayerData layer, int id, PSDLayerImportSettingSerializedPropertyWrapper property)
            : base(layer, id, property)
        {
            this.icon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);
            m_ChildFlattenCount = 0;
        }

        public int childFlattenCount => m_ChildFlattenCount;

        public override void NotifyParentOnFlattenChange()
        {
            var pp = parent as PSDNode;
            if(pp != null)
                pp.ChildGroupFlatten(flatten);
        }
        
        public override void ChildGroupFlatten(bool flatten)
        {
            m_ChildFlattenCount += flatten ? 1 : -1;
            var pp = parent as PSDNode;
            if(pp != null)
                pp.ChildGroupFlatten(flatten);
        }

        public override void FlattenStateChange()
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    var p = ((PSDNode)child);
                    if (p != null)
                    {
                        //p.disable = flatten || this.disable;
                        p.FlattenStateChange();   
                    }
                }   
            }
        }
    }

    static class Style
    {
        static public GUIStyle hoverLine = "TV Selection";
        static public GUIStyle flattenToggleStyle = "MultiColumnHeaderCenter";
        static public readonly string k_LightIconResourcePath = "Icons/Light";
        static public readonly string k_DarkIconResourcePath = "Icons/Dark";
        static public readonly string k_SelectedIconResourcePath = "Icons/Selected";
        public static readonly GUIContent layerHiddenToolTip = EditorGUIUtility.TrTextContent("", "The layer is hidden in the source file.");
        public static readonly GUIContent[] mergedIcon =
        {
            new GUIContent(LoadIconResource("Layers Separated", k_LightIconResourcePath, k_DarkIconResourcePath), L10n.Tr("Layers Separated. Click to merge them.")),
            new GUIContent(LoadIconResource("Layers Separated", k_SelectedIconResourcePath, k_SelectedIconResourcePath), L10n.Tr("Layers Separated. Click to merge them."))
        };
        public static readonly GUIContent[] separateIcon =
        {
            new GUIContent(LoadIconResource("Layers Collapsed", k_LightIconResourcePath, k_DarkIconResourcePath), L10n.Tr("Layers merged. Click to separate them.")),
            new GUIContent(LoadIconResource("Layers Collapsed", k_SelectedIconResourcePath, k_SelectedIconResourcePath), L10n.Tr("Layers merged. Click to separate them."))
        };
        public static readonly GUIContent[] mergedMix =
        {
            new GUIContent(LoadIconResource("Layers Mixed", k_LightIconResourcePath, k_DarkIconResourcePath), L10n.Tr("Group contains child groups that are merged.")),
            new GUIContent(LoadIconResource("Layers Mixed", k_SelectedIconResourcePath, k_SelectedIconResourcePath), L10n.Tr("Group contains child groups that are merged."))
        };

        const string k_ResourcePath = "Packages/com.unity.2d.psdimporter/Editor/Assets";
        
        public static int iconSize = 16;
        public static int iconPadding = 6;
        public static Texture2D LoadIconResource(string name, string personalPath, string proPath)
        {
            string iconPath = "";

            if (EditorGUIUtility.isProSkin && !string.IsNullOrEmpty(proPath))
                iconPath = Path.Combine(proPath, name);
            else
                iconPath = Path.Combine(personalPath, name);
            if (EditorGUIUtility.pixelsPerPoint > 1.0f)
            {
                var icon2x = Load<Texture2D>(iconPath + "@4x.png");
                if (icon2x != null)
                    return icon2x;
            }

            return Load<Texture2D>(iconPath+"@2x.png");
        }

        internal static T Load<T>(string path) where T : Object
        {
            var assetPath = Path.Combine(k_ResourcePath, path);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return asset;
        }

        static Style()
        {
            flattenToggleStyle.border = new RectOffset();
            flattenToggleStyle.margin = new RectOffset();
            flattenToggleStyle.padding = new RectOffset();
        }
    }
    
    internal class PSDImporterEditorLayerTreeView : IMGUI.Controls.TreeView
    {
        PSDLayerData[] m_Layers;
        bool m_ShowHidden;
        bool m_HasChanged;
        string m_RootName;
        SerializedProperty m_PSDLayerImportSetting;
        IPSDLayerMappingStrategy m_MappingStrategy;
        int m_LastArraySize;
        
        public PSDImporterEditorLayerTreeView(string rootName, TreeViewState treeViewState, PSDLayerData[] layers, bool showHidden, SerializedProperty psdLayerImportSetting, IPSDLayerMappingStrategy mappingStrategy)
            : base(treeViewState)
        {
            m_Layers = layers;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            m_ShowHidden = showHidden;
            m_HasChanged = false;
            baseIndent = 16;
            useScrollView = true;
            m_RootName = rootName;
            m_PSDLayerImportSetting = psdLayerImportSetting;
            m_MappingStrategy = mappingStrategy;
            Reload();
        }

        public bool showHidden
        {
            get => m_ShowHidden;
            set => m_ShowHidden = value;
        }

        public override void OnGUI(Rect rect)
        {
            if(m_PSDLayerImportSetting.arraySize != m_LastArraySize)
                Reload();
            base.OnGUI(rect);
            m_LastArraySize = m_PSDLayerImportSetting.arraySize;
        }
        
        protected override TreeViewItem BuildRoot()
        {
            m_PSDLayerImportSetting.serializedObject.Update();
            m_LastArraySize = m_PSDLayerImportSetting.arraySize;
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
            var fileRoot = new PSDNode(){id = -2};
            fileRoot.displayName = m_RootName;
            //fileRoot.icon = EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
            root.AddChild(fileRoot);
            var spWrapper = new List<PSDLayerImportSettingSerializedPropertyWrapper>();
            if (m_PSDLayerImportSetting.arraySize > 0)
            {
                var firstElement = m_PSDLayerImportSetting.GetArrayElementAtIndex(0);
                for (int i = 0; i < m_PSDLayerImportSetting.arraySize; ++i)
                {
                    spWrapper.Add(new PSDLayerImportSettingSerializedPropertyWrapper(firstElement, m_PSDLayerImportSetting, null));
                    firstElement.Next(false);
                }
            }
            if (m_Layers != null)
            {
                PSDNode[] nodes = new PSDNode[m_Layers.Length];
                for(int i = 0; i < m_Layers.Length; ++i)
                {
                    var l = m_Layers[i];
                    var importSettingIndex = spWrapper.FindIndex(x => m_MappingStrategy.Compare(x, l));
                    PSDLayerImportSettingSerializedPropertyWrapper importSetting = null;
                    if (importSettingIndex < 0)
                    {
                        importSetting = new PSDLayerImportSettingSerializedPropertyWrapper(null, m_PSDLayerImportSetting, l);
                    }
                    else
                    {
                        importSetting = spWrapper[importSettingIndex];
                        spWrapper.RemoveAt(importSettingIndex);
                    }
                         
                    if (l!= null && l.isGroup)
                        nodes[i] = new PSDLayerGroupNode(l, i, importSetting);
                    else
                        nodes[i] = new PSDLayerNode(l, i, importSetting);
                    var node = nodes[i];

                    node.disable = !node.layer.isVisible;
                    while (node.layer.parentIndex != -1 && nodes[i].disable == false)
                    {
                        if (!node.layer.isVisible || !nodes[node.layer.parentIndex].layer.isVisible)
                        {
                            nodes[i].disable = true;
                        }

                        node = nodes[node.layer.parentIndex];
                    }
                }
                foreach (var node in nodes)
                {
                    //if (showHidden || node.layer.isVisible)
                    {
                        if (node.layer.parentIndex == -1)
                        {
                            fileRoot.AddChild(node);
                        }
                        else
                        {
                            nodes[node.layer.parentIndex].AddChild(node);
                            if(node.flatten)
                                nodes[node.layer.parentIndex].ChildGroupFlatten(node.flatten);
                        }
                    }
                }    
            }
            SetupDepthsFromParentsAndChildren(root);
            SetExpanded(fileRoot.id, true);
            return root;
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            var node = (PSDNode)args.item;
            var rowRect = args.rowRect;
            var hover = rowRect.Contains(Event.current.mousePosition);
            var a1 = args.focused;
            var a2 = args.selected;
            if (hover && Event.current.type == EventType.Repaint)
            {
                args.selected = args.focused = true;
                Style.hoverLine.Draw(rowRect, false, false, a2, true);
            }
            using (new EditorGUI.DisabledScope(node.disable))
            {
                if (node.disable)
                {
                    var r = new Rect(rowRect.x + args.item.depth * this.depthIndentWidth + this.foldoutWidth + 32, rowRect.y, rowRect.width, rowRect.height);
                    GUI.Label(r, Style.layerHiddenToolTip);   
                }
                base.RowGUI(args);
            }
            
            args.focused = a1;
            args.selected = a2;
            
            // if (node.layer != null && !node.layer.isVisible)
            // {
            //     //GUI.Box(new Rect(rowRect.x, rowRect.y, Style.iconSize, Style.iconSize), Style.visibilityIcon, Style.flattenToggleStyle);
            // }
            
            if (args.item is PSDLayerGroupNode)
            {
                var group = (PSDLayerGroupNode)args.item;
                Rect toggleRect = new Rect(rowRect.x + args.item.depth * this.depthIndentWidth, rowRect.y, Style.iconSize, Style.iconSize);
                GUIContent[] icon = null;
                if (group.childFlattenCount != 0)
                    icon = Style.mergedMix;
                if (hover)
                {
                    if(group.flatten)
                        icon = Style.separateIcon;
                    else
                        icon = Style.mergedIcon;
                }
                else if(group.flatten)
                    icon = Style.mergedIcon;
                    
                if (icon != null)
                {
                    hover = toggleRect.Contains(Event.current.mousePosition);
                    var iconIndex = args.selected | hover ? 1 : 0;
                    EditorGUI.BeginChangeCheck();
                    var flatten = GUI.Toggle(toggleRect, group.flatten, icon[iconIndex], Style.flattenToggleStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        group.flatten = flatten;
                        group.FlattenStateChange();
                        group.NotifyParentOnFlattenChange();
                        m_HasChanged = true;
                    }
                }
            }
        }
        
        public bool GetHasChangeAndClear()
        {
            var v = m_HasChanged;
            m_HasChanged = false;
            return v;
        }
    }
}

