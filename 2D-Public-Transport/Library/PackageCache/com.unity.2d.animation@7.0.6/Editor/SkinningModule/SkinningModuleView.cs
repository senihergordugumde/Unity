using System;
using UnityEditor.U2D.Common;
using UnityEditor.U2D.Layout;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal partial class SkinningModule
    {
        private LayoutOverlay m_LayoutOverlay;
        private PoseToolbar m_PoseToolbar;
        private BoneToolbar m_BoneToolbar;
        private MeshToolbar m_MeshToolbar;
        private WeightToolbar m_WeightToolbar;
        private RigToolbar m_RigToolbar;

        private InternalEditorBridge.ShortcutContext m_ShortcutContext;

        private static SkinningModule GetModuleFromContext(ShortcutArguments args)
        {
            var sc = args.context as InternalEditorBridge.ShortcutContext;
            if (sc == null)
                return null;

            return sc.context as SkinningModule;
        }

        [Shortcut(ShortcutIds.toggleToolText, typeof(InternalEditorBridge.ShortcutContext), KeyCode.BackQuote, ShortcutModifiers.Shift)]
        private static void CollapseToolbar(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null)
            {
                SkinningModuleSettings.compactToolBar = !SkinningModuleSettings.compactToolBar;
            }
        }

        [Shortcut(ShortcutIds.restoreBindPose, typeof(InternalEditorBridge.ShortcutContext), KeyCode.Alpha1, ShortcutModifiers.Shift)]
        private static void DisablePoseModeKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                var effectiveSkeleton = sm.skinningCache.GetEffectiveSkeleton(sm.skinningCache.selectedSprite);
                if (effectiveSkeleton != null && effectiveSkeleton.isPosePreview)
                {
                    using (sm.skinningCache.UndoScope(TextContent.restorePose))
                    {
                        sm.skinningCache.RestoreBindPose();
                        sm.skinningCache.events.shortcut.Invoke("#1");
                    }   
                }
            }
        }

        [Shortcut(ShortcutIds.toggleCharacterMode, typeof(InternalEditorBridge.ShortcutContext), KeyCode.Alpha2, ShortcutModifiers.Shift)]
        private static void ToggleCharacterModeKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.hasCharacter)
            {
                var tool = sm.skinningCache.GetTool(Tools.SwitchMode);

                using (sm.skinningCache.UndoScope(TextContent.setMode))
                {
                    if (tool.isActive)
                        tool.Deactivate();
                    else
                        tool.Activate();
                }

                sm.skinningCache.events.shortcut.Invoke("#2");
            }
        }

        [Shortcut(ShortcutIds.previewPose, typeof(InternalEditorBridge.ShortcutContext), KeyCode.Q, ShortcutModifiers.Shift)]
        private static void EditPoseKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.EditPose);
                sm.skinningCache.events.shortcut.Invoke("#q");
            }
        }

        [Shortcut(ShortcutIds.editBone, typeof(InternalEditorBridge.ShortcutContext), KeyCode.W, ShortcutModifiers.Shift)]
        private static void EditJointsKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.EditJoints);
                sm.skinningCache.events.shortcut.Invoke("#w");
            }
        }

        [Shortcut(ShortcutIds.createBone, typeof(InternalEditorBridge.ShortcutContext), KeyCode.E, ShortcutModifiers.Shift)]
        private static void CreateBoneKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.CreateBone);
                sm.skinningCache.events.shortcut.Invoke("#e");
            }
        }

        [Shortcut(ShortcutIds.splitBone, typeof(InternalEditorBridge.ShortcutContext), KeyCode.R, ShortcutModifiers.Shift)]
        private static void SplitBoneKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.SplitBone);
                sm.skinningCache.events.shortcut.Invoke("#r");
            }
        }

        [Shortcut(ShortcutIds.autoGeometry, typeof(InternalEditorBridge.ShortcutContext), KeyCode.A, ShortcutModifiers.Shift)]
        private static void GenerateGeometryKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.GenerateGeometry);
                sm.skinningCache.events.shortcut.Invoke("#a");
            }
        }

        [Shortcut(ShortcutIds.editGeometry, typeof(InternalEditorBridge.ShortcutContext), KeyCode.S, ShortcutModifiers.Shift)]
        private static void MeshSelectionKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.EditGeometry);
                sm.skinningCache.events.shortcut.Invoke("#s");
            }
        }

        [Shortcut(ShortcutIds.createVertex, typeof(InternalEditorBridge.ShortcutContext), KeyCode.J, ShortcutModifiers.Shift)]
        private static void CreateVertex(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.CreateVertex);
                sm.skinningCache.events.shortcut.Invoke("#d");
            }
        }

        [Shortcut(ShortcutIds.createEdge, typeof(InternalEditorBridge.ShortcutContext), KeyCode.G, ShortcutModifiers.Shift)]
        private static void CreateEdgeKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.CreateEdge);
                sm.skinningCache.events.shortcut.Invoke("#g");
            }
        }

        [Shortcut(ShortcutIds.splitEdge, typeof(InternalEditorBridge.ShortcutContext), KeyCode.H, ShortcutModifiers.Shift)]
        private static void SplitEdge(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.SplitEdge);
                sm.skinningCache.events.shortcut.Invoke("#h");
            }
        }

        [Shortcut(ShortcutIds.autoWeights, typeof(InternalEditorBridge.ShortcutContext), KeyCode.Z, ShortcutModifiers.Shift)]
        private static void GenerateWeightsKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.GenerateWeights);
                sm.skinningCache.events.shortcut.Invoke("#z");
            }
        }

        [Shortcut(ShortcutIds.weightSlider, typeof(InternalEditorBridge.ShortcutContext), KeyCode.X, ShortcutModifiers.Shift)]
        private static void WeightSliderKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.WeightSlider);
                sm.skinningCache.events.shortcut.Invoke("#x");
            }
        }

        [Shortcut(ShortcutIds.weightBrush, typeof(InternalEditorBridge.ShortcutContext), KeyCode.N, ShortcutModifiers.Shift)]
        private static void WeightBrushKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.WeightBrush);
                sm.skinningCache.events.shortcut.Invoke("#c");
            }
        }

        [Shortcut(ShortcutIds.boneInfluence, typeof(InternalEditorBridge.ShortcutContext), KeyCode.V, ShortcutModifiers.Shift)]
        private static void BoneInfluenceKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.mode == SkinningMode.Character)
            {
                sm.SetWeightTool(Tools.BoneInfluence);
                sm.skinningCache.events.shortcut.Invoke("#v");
            }
        }
        
        [Shortcut(ShortcutIds.spriteInfluence, typeof(InternalEditorBridge.ShortcutContext), KeyCode.M, ShortcutModifiers.Shift)]
        private static void SpriteInfluenceKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.mode == SkinningMode.Character)
            {
                sm.SetWeightTool(Tools.SpriteInfluence);
                sm.skinningCache.events.shortcut.Invoke("#m");
            }
        }

        [Shortcut(ShortcutIds.pastePanelWeights, typeof(InternalEditorBridge.ShortcutContext), KeyCode.B, ShortcutModifiers.Shift)]
        private static void PastePanelKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.TogglePasteTool();
                sm.skinningCache.events.shortcut.Invoke("#b");
            }
        }

        [Shortcut(ShortcutIds.visibilityPanel, typeof(InternalEditorBridge.ShortcutContext), KeyCode.P, ShortcutModifiers.Shift)]
        private static void VisibilityPanelKey(ShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.m_HorizontalToggleTools.ToggleVisibilityTool(sm.currentTool);
                sm.skinningCache.events.shortcut.Invoke("#p");
            }
        }

        private void AddMainUI(VisualElement mainView)
        {
            var visualTree = ResourceLoader.Load<VisualTreeAsset>("LayoutOverlay/LayoutOverlay.uxml");
            VisualElement clone = visualTree.CloneTree();
            m_LayoutOverlay = clone.Q<LayoutOverlay>("LayoutOverlay");

            mainView.Add(m_LayoutOverlay);
            m_LayoutOverlay.hasScrollbar = true;
            m_LayoutOverlay.verticalToolbar.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_LayoutOverlay.StretchToParentSize();

            CreatePoseToolbar();
            CreateBoneToolbar();
            CreateMeshToolbar();
            CreateWeightToolbar();
            CreateRigToolbar();

            m_ShortcutContext = new InternalEditorBridge.ShortcutContext()
            {
                isActive = isFocused,
                context = this
            };
            InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
            InternalEditorBridge.AddEditorApplicationProjectLoadedCallback(OnProjectLoaded);
        }

        private void OnProjectLoaded()
        {
            if (m_ShortcutContext != null)
                InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
        }
        
        private void DoViewGUI()
        {
            if (spriteEditor.editingDisabled == m_BoneToolbar.enabledSelf)
            {
                m_BoneToolbar.SetEnabled(!spriteEditor.editingDisabled);
                m_MeshToolbar.SetEnabled(!spriteEditor.editingDisabled);
                m_WeightToolbar.SetEnabled(!spriteEditor.editingDisabled);
            }

            if (spriteEditor.editingDisabled == m_LayoutOverlay.rightOverlay.enabledSelf)
            {
                m_LayoutOverlay.rightOverlay.SetEnabled(!spriteEditor.editingDisabled);
                m_LayoutOverlay.rightOverlay.visible = !spriteEditor.editingDisabled;
            }
            
            m_PoseToolbar.UpdateResetButtonState();
            m_RigToolbar.UpdatePasteButtonEnabledState();
        }

        private bool isFocused()
        {
            return spriteEditor != null && (EditorWindow.focusedWindow == spriteEditor as EditorWindow);
        }

        private void CreatePoseToolbar()
        {
            m_PoseToolbar = PoseToolbar.GenerateFromUXML();
            m_PoseToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_PoseToolbar);
            
            m_PoseToolbar.ActivateEditPoseTool += ActivateEditPoseTool;
            m_PoseToolbar.SetEnabled(!spriteEditor.editingDisabled);            
        }
        
        private void CreateBoneToolbar()
        {
            m_BoneToolbar = BoneToolbar.GenerateFromUXML();
            m_BoneToolbar.Setup(skinningCache);
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_BoneToolbar);

            m_BoneToolbar.SetSkeletonTool += SetSkeletonTool;
            m_BoneToolbar.SetEnabled(!spriteEditor.editingDisabled);
        }

        private void CreateMeshToolbar()
        {
            m_MeshToolbar = MeshToolbar.GenerateFromUXML();
            m_MeshToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_MeshToolbar);

            m_MeshToolbar.SetMeshTool += SetMeshTool;
            m_MeshToolbar.SetEnabled(!spriteEditor.editingDisabled);
        }

        private void CreateWeightToolbar()
        {
            m_WeightToolbar = WeightToolbar.GenerateFromUXML();
            m_WeightToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_WeightToolbar);
            m_WeightToolbar.SetWeightTool += SetWeightTool;
            m_WeightToolbar.SetEnabled(!spriteEditor.editingDisabled);
        }

        private void CreateRigToolbar()
        {
            m_RigToolbar = RigToolbar.GenerateFromUXML();
            m_RigToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_RigToolbar);  
            
            m_RigToolbar.ActivateCopyTool += ActivateCopyTool;
            m_RigToolbar.TogglePasteTool += TogglePasteTool;
            m_RigToolbar.SetEnabled(!spriteEditor.editingDisabled);            
        }

        private void ActivateEditPoseTool()
        {
            var tool = skinningCache.GetTool(Tools.EditPose);
            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);
            }
        }

        private void SetSkeletonTool(Tools toolType)
        {
            var tool = skinningCache.GetTool(toolType) as SkeletonToolWrapper;

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);

                if (tool.editBindPose)
                    skinningCache.RestoreBindPose();
            }
        }

        private void SetMeshTool(Tools toolType)
        {
            var tool  = skinningCache.GetTool(toolType);

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);
                skinningCache.RestoreBindPose();
                UnselectBones();
            }
        }

        private void SetWeightTool(Tools toolType)
        {
            var tool = skinningCache.GetTool(toolType);

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);
            }
        }

        private void ActivateCopyTool()
        {
            var tool = skinningCache.GetTool(Tools.CopyPaste) as CopyTool;
            tool.OnCopyActivated();
        }

        private void TogglePasteTool()
        {
            var tool = skinningCache.GetTool(Tools.CopyPaste) as CopyTool;
            if (!tool.isActive)
                ActivateTool(tool);
            else if (previousTool != null)
                ActivateTool(previousTool);
        }
        
        private void StorePreviousTool()
        {
            if (currentTool is CopyTool || currentTool is VisibilityTool)
                return;
            
            previousTool = currentTool;
        }

        private void ActivateTool(BaseTool tool)
        {
            StorePreviousTool();
            
            m_ModuleToolGroup.ActivateTool(tool);
            UpdateToggleState();
            skinningCache.events.toolChanged.Invoke(tool);
        }

        private void UnselectBones()
        {
            skinningCache.skeletonSelection.Clear();
            skinningCache.events.boneSelectionChanged.Invoke();
        }

        private void UpdateToggleState()
        {
            Debug.Assert(m_PoseToolbar != null);
            Debug.Assert(m_BoneToolbar != null);
            Debug.Assert(m_MeshToolbar != null);
            Debug.Assert(m_WeightToolbar != null);

            m_PoseToolbar.UpdatePreviewButtonCheckedState();
            m_BoneToolbar.UpdateToggleState();
            m_MeshToolbar.UpdateToggleState();
            m_WeightToolbar.UpdateToggleState();
            m_RigToolbar.UpdatePasteButtonCheckedState();
        }

        private void RemoveMainUI(VisualElement mainView)
        {
            InternalEditorBridge.RemoveEditorApplicationProjectLoadedCallback(OnProjectLoaded);
            InternalEditorBridge.UnregisterShortcutContext(m_ShortcutContext);
        }
    }
}
