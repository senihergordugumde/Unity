using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class BoneToolbar : Toolbar
    {
        private const string k_UxmlPath = "SkinningModule/BoneToolbar.uxml";
        private const string k_ToolbarId = "BoneToolbar";        
        
        private const string k_EditJointsId = "EditJoints";
        private const string k_CreateBoneId = "CreateBone";
        private const string k_SplitBoneId = "SplitBone";
        
        public class BoneToolbarFactory : UxmlFactory<BoneToolbar, BoneToolbarUxmlTraits> {}
        public class BoneToolbarUxmlTraits : UxmlTraits {}

        public event Action<Tools> SetSkeletonTool = (mode) => {};
        public SkinningCache skinningCache { get; private set; }

        public static BoneToolbar GenerateFromUXML()
        {
            var clone = GetClone(k_UxmlPath, k_ToolbarId) as BoneToolbar;
            clone.BindElements();
            clone.SetupShortcutUtility();
            clone.LocalizeTextInChildren();
            clone.AddShortcutsToToolTips();
            return clone;
        }
        
        public BoneToolbar()
        {
            styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/BoneToolbarStyle.uss"));
        }

        private void BindElements()
        {
            var editJoints = this.Q<Button>(k_EditJointsId);
            editJoints.clickable.clicked += () => { SetSkeletonTool(Tools.EditJoints); };

            var createBone = this.Q<Button>(k_CreateBoneId);
            createBone.clickable.clicked += () => { SetSkeletonTool(Tools.CreateBone); };

            var splitBone = this.Q<Button>(k_SplitBoneId);
            splitBone.clickable.clicked += () => { SetSkeletonTool(Tools.SplitBone); };
        }
        
        private void SetupShortcutUtility()
        {
            m_ShortcutUtility = new ShortcutUtility(ShortcutIds.editBone,
                                                    ShortcutIds.createBone,
                                                    ShortcutIds.splitBone);
            m_ShortcutUtility.OnShortcutChanged = () =>
            {
                RestoreButtonTooltips(k_UxmlPath, k_ToolbarId);
                AddShortcutsToToolTips();
            };            
        }         

        public void Setup(SkinningCache s)
        {
            skinningCache = s;
            skinningCache.events.skinningModeChanged.AddListener(OnSkinningModeChange);
        }

        private void OnSkinningModeChange(SkinningMode mode)
        {
            if (skinningCache.hasCharacter)
            {
                if (mode == SkinningMode.SpriteSheet || skinningCache.bonesReadOnly)
                {
                    this.Q<Button>(k_EditJointsId).SetEnabled(false);
                    this.Q<Button>(k_CreateBoneId).SetEnabled(false);
                    this.Q<Button>(k_SplitBoneId).SetEnabled(false);
                    
                    if (skinningCache.GetTool(Tools.EditJoints).isActive
                        || skinningCache.GetTool(Tools.CreateBone).isActive
                        || skinningCache.GetTool(Tools.SplitBone).isActive)
                        SetSkeletonTool(Tools.EditPose);
                }
                else if (mode == SkinningMode.Character)
                {
                    this.Q<Button>(k_EditJointsId).SetEnabled(true);
                    this.Q<Button>(k_CreateBoneId).SetEnabled(true);
                    this.Q<Button>(k_SplitBoneId).SetEnabled(true);
                }
            }
        }

        public void UpdateToggleState()
        {
            //TODO: Make UI not be aware of BaseTool, Cache, etc. Use Tool enum
            var toolButton = this.Q<Button>(k_EditJointsId);
            SetButtonChecked(toolButton, skinningCache.GetTool(Tools.EditJoints).isActive);

            toolButton = this.Q<Button>(k_CreateBoneId);
            SetButtonChecked(toolButton, skinningCache.GetTool(Tools.CreateBone).isActive);

            toolButton = this.Q<Button>(k_SplitBoneId);
            SetButtonChecked(toolButton, skinningCache.GetTool(Tools.SplitBone).isActive);

            OnSkinningModeChange(skinningCache.mode);
        }
        
        private void AddShortcutsToToolTips()
        {
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_EditJointsId, ShortcutIds.editBone);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_CreateBoneId, ShortcutIds.createBone);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_SplitBoneId, ShortcutIds.splitBone);
        }
    }
}
