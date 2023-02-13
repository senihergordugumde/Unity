using System;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class PoseToolbar : Toolbar
    {
        private const string k_UxmlPath = "SkinningModule/PoseToolbar.uxml";
        private const string k_UssPath = "SkinningModule/PoseToolbarStyle.uss";
        private const string k_ToolbarId = "PoseToolbar";

        private const string k_PreviewPoseId = "PreviewPose";
        private const string k_RestorePoseId = "RestorePose";

        public class CustomUXMLFactor : UxmlFactory<PoseToolbar, UxmlTraits> {}

        public event Action ActivateEditPoseTool = () => {};
        
        public SkinningCache skinningCache { get; set; }      
        
        private Button m_PreviewBtn;
        private Button m_RestoreBtn;        
        
        public static PoseToolbar GenerateFromUXML()
        {
            var clone = GetClone(k_UxmlPath, k_ToolbarId) as PoseToolbar;
            clone.BindElements();
            clone.SetupShortcutUtility();
            clone.LocalizeTextInChildren();
            clone.AddShortcutsToToolTips();
            return clone;
        }
        
        public PoseToolbar()
        {
            styleSheets.Add(ResourceLoader.Load<StyleSheet>(k_UssPath));
        }

        private void BindElements()
        {
            m_PreviewBtn = this.Q<Button>(k_PreviewPoseId);
            m_PreviewBtn.clickable.clicked += () => { ActivateEditPoseTool(); };
            
            m_RestoreBtn = this.Q<Button>(k_RestorePoseId);
            m_RestoreBtn.clickable.clicked += RestorePose;
        }

        private void RestorePose()
        {
            using (skinningCache.UndoScope(TextContent.restorePose))
            {
                skinningCache.RestoreBindPose();
                skinningCache.events.restoreBindPose.Invoke();
            }
        }
        
        private void SetupShortcutUtility()
        {
            m_ShortcutUtility = new ShortcutUtility(ShortcutIds.previewPose,
                                                    ShortcutIds.restoreBindPose);
            m_ShortcutUtility.OnShortcutChanged = () =>
            {
                RestoreButtonTooltips(k_UxmlPath, k_ToolbarId);
                AddShortcutsToToolTips();
            };            
        }

        public void UpdatePreviewButtonCheckedState()
        {
            SetButtonChecked(m_PreviewBtn, skinningCache.GetTool(Tools.EditPose).isActive);
        }

        public void UpdateResetButtonState()
        {
            var skeleton = skinningCache.GetEffectiveSkeleton(skinningCache.selectedSprite);
            var isResetEnabled = skeleton != null && skeleton.isPosePreview;
            m_RestoreBtn.SetEnabled(isResetEnabled);
        }
        
        private void AddShortcutsToToolTips()
        {
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_PreviewPoseId, ShortcutIds.previewPose);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_RestorePoseId, ShortcutIds.restoreBindPose);
        }        
    }
}
