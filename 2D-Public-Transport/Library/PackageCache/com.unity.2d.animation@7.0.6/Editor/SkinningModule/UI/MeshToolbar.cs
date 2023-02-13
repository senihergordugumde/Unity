using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class MeshToolbar : Toolbar
    {
        private const string k_UxmlPath = "SkinningModule/MeshToolbar.uxml";
        private const string k_ToolbarId = "MeshToolbar";
        
        private const string k_SelectGeometryId = "SelectGeometry";
        private const string k_CreateVertexId = "CreateVertex";
        private const string k_CreateEdgeId = "CreateEdge";
        private const string k_SplitEdgeId = "SplitEdge";
        private const string k_GenerateGeometryId = "GenerateGeometry";
        
        public class MeshToolbarFactory : UxmlFactory<MeshToolbar, MeshToolbarUxmlTraits> {}
        public class MeshToolbarUxmlTraits : UxmlTraits {}

        public event Action<Tools> SetMeshTool = (mode) => {};
        public SkinningCache skinningCache { get; set; }

        public static MeshToolbar GenerateFromUXML()
        {
            var clone = GetClone(k_UxmlPath, k_ToolbarId) as MeshToolbar;
            clone.BindElements();
            clone.SetupShortcutUtility();
            clone.LocalizeTextInChildren();
            clone.AddShortcutsToToolTips();
            return clone;
        }        
        
        public MeshToolbar()
        {
            styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/MeshToolbarStyle.uss"));
        }

        private void BindElements()
        {
            var button = this.Q<Button>(k_SelectGeometryId);
            button.clickable.clicked += () => { SetMeshTool(Tools.EditGeometry); };

            button = this.Q<Button>(k_CreateVertexId);
            button.clickable.clicked += () => { SetMeshTool(Tools.CreateVertex); };

            button = this.Q<Button>(k_CreateEdgeId);
            button.clickable.clicked += () => { SetMeshTool(Tools.CreateEdge); };

            button = this.Q<Button>(k_SplitEdgeId);
            button.clickable.clicked += () => { SetMeshTool(Tools.SplitEdge); };

            button = this.Q<Button>(k_GenerateGeometryId);
            button.clickable.clicked += () => { SetMeshTool(Tools.GenerateGeometry); };
        }
        
        private void SetupShortcutUtility()
        {
            m_ShortcutUtility = new ShortcutUtility(ShortcutIds.editGeometry,
                ShortcutIds.createVertex,
                ShortcutIds.createEdge,
                ShortcutIds.splitEdge,
                ShortcutIds.autoGeometry);
            m_ShortcutUtility.OnShortcutChanged = () =>
            {
                RestoreButtonTooltips(k_UxmlPath, k_ToolbarId);
                AddShortcutsToToolTips();
            };            
        }

        public void UpdateToggleState()
        {
            //TODO: Make UI not be aware of BaseTool, Cache, etc. Use Tool enum
            var button = this.Q<Button>(k_SelectGeometryId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.EditGeometry).isActive);

            button = this.Q<Button>(k_CreateVertexId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.CreateVertex).isActive);

            button = this.Q<Button>(k_CreateEdgeId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.CreateEdge).isActive);

            button = this.Q<Button>(k_SplitEdgeId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.SplitEdge).isActive);

            button = this.Q<Button>(k_GenerateGeometryId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.GenerateGeometry).isActive);
        }
        
        private void AddShortcutsToToolTips()
        {
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_SelectGeometryId, ShortcutIds.editGeometry);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_CreateVertexId, ShortcutIds.createVertex);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_CreateEdgeId, ShortcutIds.createEdge);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_SplitEdgeId, ShortcutIds.splitEdge);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_GenerateGeometryId, ShortcutIds.autoGeometry);
        }
    }
}
