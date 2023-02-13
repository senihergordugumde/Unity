using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Layout;

namespace UnityEditor.U2D.Animation
{
    internal class SpriteBoneInfluenceToolController
    {
        SkinningEvents m_Events;
        ISpriteBoneInfluenceToolModel m_Model;
        public SpriteBoneInfluenceToolController(ISpriteBoneInfluenceToolModel model, SkinningEvents events)
        {
            m_Events = events;
            m_Model = model;
        }

        public void Activate()
        {
            m_Events.selectedSpriteChanged.AddListener(OnSpriteSelectionChanged);
            m_Events.boneSelectionChanged.AddListener(OnBoneSelectionChanged);
            m_Events.boneNameChanged.AddListener(OnBoneNameChanged);
            m_Events.skeletonTopologyChanged.AddListener(OnSkeletonTopologyChanged);
            m_Events.meshChanged.AddListener(OnMeshChanged);
            m_Events.skinningModeChanged.AddListener(OnSkinningModeChanged);
            
            ShowHideView(true);
        }

        public void Deactivate()
        {
            m_Events.selectedSpriteChanged.RemoveListener(OnSpriteSelectionChanged);
            m_Events.boneSelectionChanged.RemoveListener(OnBoneSelectionChanged);
            m_Events.boneNameChanged.RemoveListener(OnBoneNameChanged);
            m_Events.skeletonTopologyChanged.RemoveListener(OnSkeletonTopologyChanged);
            m_Events.meshChanged.RemoveListener(OnMeshChanged);
            m_Events.skinningModeChanged.RemoveListener(OnSkinningModeChanged);
            
            ShowHideView(false);
        }

        void OnMeshChanged(MeshCache mesh)
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                UpdateAddRemoveButtons();
            }
        }

        void OnSpriteSelectionChanged(SpriteCache sprite)
        {
            if (m_Model.view.visible)
            {
                UpdateSelectedSpriteBoneInfluence();
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                UpdateAddRemoveButtons();
                SetViewHeaderText();
            }
        }

        void OnBoneSelectionChanged()
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                UpdateAddRemoveButtons();
            }
        }

        void OnBoneNameChanged(BoneCache bone)
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
            }
        }

        void OnSkeletonTopologyChanged(SkeletonCache skeleton)
        {
            if (m_Model.view.visible)
            {
                UpdateSelectedSpriteBoneInfluence();
                 
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
            }
        }

        void UpdateAddRemoveButtons()
        {
            m_Model.view.ToggleAddButton(ShouldEnableAddButton());
            m_Model.view.ToggleRemoveButton(InCharacterMode());
        }

        public void OnViewCreated()
        {
            m_Model.view.onAddElement += AddSelectedBoneInfluenceToSprite;
            m_Model.view.onRemoveElement += RemoveSelectedBoneInfluenceFromSprite;
            m_Model.view.onReordered += OnReorderBoneInfluenceFromSprite;
            m_Model.view.onSelectionChanged += SelectedBonesFromList;
            
            ShowHideView(false);
        }
        
        void OnSkinningModeChanged(SkinningMode skinningMode)
        {
            UpdateAddRemoveButtons();
        }

        void AddSelectedBoneInfluenceToSprite()
        {
            var character = m_Model.characterSkeleton;

            if (character == null)
                return;

            var characterPart = m_Model.GetSpriteCharacterPart(m_Model.selectedSprite);
            var selectedBones = m_Model.selectedBones;
            var characterBones = characterPart.bones.ToList();

            foreach (var bone in selectedBones)
            {
                if (!characterBones.Contains(bone))
                    characterBones.Add(bone as BoneCache);
            }

            using (m_Model.UndoScope(TextContent.addBoneInfluence))
            {
                characterPart.bones = characterBones.ToArray();
                m_Events.characterPartChanged.Invoke(characterPart);
                
                UpdateSelectedSpriteBoneInfluence();
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                UpdateAddRemoveButtons();
            }
        }

        void RemoveSelectedBoneInfluenceFromSprite()
        {
            var character = m_Model.characterSkeleton;

            if (character == null)
                return;

            var characterPart = m_Model.GetSpriteCharacterPart(m_Model.selectedSprite);
            var selectedBones = m_Model.selectedBones;
            var characterBones = characterPart.bones.ToList();

            characterBones.RemoveAll(b => selectedBones.Contains(b));

            using (m_Model.UndoScope(TextContent.removeBoneInfluence))
            {
                characterPart.bones = characterBones.ToArray();
                m_Events.characterPartChanged.Invoke(characterPart);
                
                characterPart.sprite.SmoothFill();
                m_Events.meshChanged.Invoke(characterPart.sprite.GetMesh());

                UpdateSelectedSpriteBoneInfluence();
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                UpdateAddRemoveButtons();
            }
        }

        void OnReorderBoneInfluenceFromSprite(IEnumerable<TransformCache> transformCache)
        {
            var boneCache = transformCache.Cast<BoneCache>();

            var character = m_Model.characterSkeleton;

            if (character != null)
            {
                var characterPart = m_Model.GetSpriteCharacterPart(m_Model.selectedSprite);
                using (m_Model.UndoScope(TextContent.reorderBoneInfluence))
                {
                    characterPart.bones = boneCache.ToArray();
                    m_Events.characterPartChanged.Invoke(characterPart);
                    
                    UpdateSelectedSpriteBoneInfluence();
                    m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                }
            }
            else
            {
                using (m_Model.UndoScope(TextContent.reorderBoneInfluence))
                {
                    m_Model.selectedSprite.GetSkeleton().ReorderBones(boneCache);
                    m_Events.skeletonTopologyChanged.Invoke(m_Model.selectedSprite.GetSkeleton());
                }
            }
        }

        void SelectedBonesFromList(IEnumerable<object> selectedBones)
        {
            using (m_Model.UndoScope(TextContent.boneSelection))
            {
                m_Model.selectedBones = selectedBones.Cast<BoneCache>();
                m_Events.boneSelectionChanged.Invoke();
            }
        }

        private void ShowHideView(bool show)
        {
            m_Model.view.SetHiddenFromLayout(!show);
            if (show)
            {
                UpdateSelectedSpriteBoneInfluence();
                m_Model.view.UpdateList(m_Model.selectionInfluencedBones);
                m_Model.view.UpdateSelection(m_Model.selectedBones);
                m_Model.view.listLabelText = TextContent.boneInfluences;
                m_Model.view.SetTooltips(TextContent.addBoneInfluenceTooltip, TextContent.removeBoneInfluenceTooltip);
                UpdateAddRemoveButtons();
                SetViewHeaderText();
            }
        }

        void SetViewHeaderText()
        {
            var headerText = m_Model.selectedSprite != null ? m_Model.selectedSprite.name : TextContent.noSpriteSelected;
            m_Model.view.headerText = headerText;
        }

        void UpdateSelectedSpriteBoneInfluence()
        {
            var selectedSpriteInfluences = new List<TransformCache>();
            var selectedSprite = m_Model.selectedSprite;

            if (selectedSprite != null)
            {
                if (m_Model.hasCharacter)
                {
                    var characterPart = m_Model.GetSpriteCharacterPart(selectedSprite);
                    selectedSpriteInfluences = characterPart.bones.Cast<TransformCache>().ToList();
                }
                else
                {
                    selectedSpriteInfluences = selectedSprite.GetSkeleton().bones.Cast<TransformCache>().ToList();
                }
            }

            m_Model.selectionInfluencedBones = selectedSpriteInfluences;
        }

        public bool ShouldEnableAddButton()
        {
            if (InCharacterMode())
            {
                var hasSelectedSprite = m_Model.selectedSprite != null;
                var selectedBones = m_Model.selectedBones;
                return hasSelectedSprite && selectedBones.FirstOrDefault(x => !m_Model.selectionInfluencedBones.Contains(x)) != null;
            }
            return false;
        }

        private bool InCharacterMode()
        {
            return m_Model.hasCharacter && m_Model.skinningMode == SkinningMode.Character;
        }
    }

    internal interface ISpriteBoneInfluenceToolModel
    {
        IInfluenceWindow view { get; }
        IEnumerable<BoneCache> selectedBones { get; set; }
        List<TransformCache> selectionInfluencedBones { get; set; }
        SpriteCache selectedSprite { get; }
        bool hasCharacter { get; }
        SkinningMode skinningMode { get; }
        SkeletonCache characterSkeleton { get; }
        UndoScope UndoScope(string description);
        CharacterPartCache GetSpriteCharacterPart(SpriteCache sprite);
    }

    internal class SpriteBoneInfluenceTool : BaseTool, ISpriteBoneInfluenceToolModel
    {
        private SpriteBoneInfluenceToolController m_Controller;
        private MeshPreviewBehaviour m_MeshPreviewBehaviour = new MeshPreviewBehaviour();
        private InfluenceWindow m_View;

        public SkeletonTool skeletonTool { set; private get; }
        public override IMeshPreviewBehaviour previewBehaviour => m_MeshPreviewBehaviour;

        internal override void OnCreate()
        {
            m_Controller = new SpriteBoneInfluenceToolController(this, skinningCache.events);
        }

        IInfluenceWindow ISpriteBoneInfluenceToolModel.view => m_View;

        IEnumerable<BoneCache> ISpriteBoneInfluenceToolModel.selectedBones
        {
            get => skinningCache.skeletonSelection.elements;
            set => skinningCache.skeletonSelection.elements = value.ToArray();
        }

        List<TransformCache> ISpriteBoneInfluenceToolModel.selectionInfluencedBones { get; set; }
        
        SpriteCache ISpriteBoneInfluenceToolModel.selectedSprite => skinningCache.selectedSprite;
        bool ISpriteBoneInfluenceToolModel.hasCharacter => skinningCache.hasCharacter;
        SkinningMode ISpriteBoneInfluenceToolModel.skinningMode => skinningCache.mode;
        SkeletonCache ISpriteBoneInfluenceToolModel.characterSkeleton => skinningCache.character != null ? skinningCache.character.skeleton : null;

        UndoScope ISpriteBoneInfluenceToolModel.UndoScope(string description) { return skinningCache.UndoScope(description); }

        protected override void OnActivate()
        {
            m_Controller.Activate();
            if (skeletonTool != null)
                skeletonTool.Activate();
        }

        protected override void OnDeactivate()
        {
            m_Controller.Deactivate();
            if (skeletonTool != null)
                skeletonTool.Deactivate();
        }

        public override void Initialize(LayoutOverlay layout)
        {
            if (m_View == null)
            {
                m_View = InfluenceWindow.CreateFromUxml();
                m_View.SetListReorderable(true);
                m_View.SetAllowMultiselect(true);
                m_View.LocalizeTextInChildren();
                m_Controller.OnViewCreated();
            }

            layout.rightOverlay.Add(m_View);
        }

        protected override void OnGUI()
        {
            m_MeshPreviewBehaviour.showWeightMap = true;
            m_MeshPreviewBehaviour.overlaySelected = true;
            m_MeshPreviewBehaviour.drawWireframe = true;

            skeletonTool.skeletonStyle = SkeletonStyles.WeightMap;
            skeletonTool.mode = SkeletonMode.EditPose;
            skeletonTool.editBindPose = false;
            skeletonTool.DoGUI();
        }

        public CharacterPartCache GetSpriteCharacterPart(SpriteCache sprite)
        {
            return sprite.GetCharacterPart();
        }
    }
}
