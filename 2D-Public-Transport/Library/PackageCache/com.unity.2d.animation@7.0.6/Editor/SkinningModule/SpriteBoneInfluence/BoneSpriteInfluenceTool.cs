using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Layout;

namespace UnityEditor.U2D.Animation
{
    internal class BoneSpriteInfluenceToolController
    {
        SkinningEvents m_Events;
        IBoneSpriteInfluenceToolModel m_Model;

        public BoneSpriteInfluenceToolController(IBoneSpriteInfluenceToolModel model, SkinningEvents events)
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

        void OnSpriteSelectionChanged(SpriteCache sprite)
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                UpdateAddRemoveButtons();
            }
        }

        void OnBoneSelectionChanged()
        {
            if (m_Model.view.visible)
            {
                UpdateSelectedBonesInfluencedSprites();
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                UpdateAddRemoveButtons();
                SetViewHeaderText();
            }
        }

        void OnBoneNameChanged(BoneCache bone)
        {
            if (m_Model.view.visible)
            {
                SetViewHeaderText();
            }
        }

        void OnSkeletonTopologyChanged(SkeletonCache skeleton)
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
            }
        }

        void OnMeshChanged(MeshCache mesh)
        {
            if (m_Model.view.visible)
            {
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                UpdateAddRemoveButtons();
            }
        }

        void OnSkinningModeChanged(SkinningMode skinningMode)
        {
            UpdateAddRemoveButtons();
        }

        void ShowHideView(bool show)
        {
            m_Model.view.SetHiddenFromLayout(!show);
            if (show)
            {
                UpdateSelectedBonesInfluencedSprites();
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                m_Model.view.listLabelText = TextContent.influencedSprites;
                m_Model.view.SetTooltips(TextContent.addSpriteInfluenceTooltip, TextContent.removeSpriteInfluenceTooltip);
                UpdateAddRemoveButtons();
                SetViewHeaderText();
            }
        }
        
        void SetViewHeaderText()
        {
            var headerText = m_Model.selectedBone != null ? m_Model.selectedBone.name : TextContent.noBoneSelected;
            m_Model.view.headerText = headerText;
        }
        
        public void OnViewCreated()
        {
            m_Model.view.onAddElement += AddSelectedSpriteInfluenceToBone;
            m_Model.view.onRemoveElement += RemoveSelectedSpriteInfluenceFromBone;
            m_Model.view.onSelectionChanged += SelectSprite;

            ShowHideView(false);
        }
        
        void AddSelectedSpriteInfluenceToBone()
        {
            if(m_Model.selectedSprite == null)
                return;

            var character = m_Model.characterSkeleton;
            
            if (character == null)
                return;
            
            if(m_Model.influencedSprites.Contains(m_Model.selectedSprite))
                return;

            var characterPart = m_Model.GetSpriteCharacterPart(m_Model.selectedSprite);
            var characterBones = characterPart.bones.ToList();
            var selectedBone = m_Model.selectedBone;
            characterBones.Add(selectedBone);
            
            using (m_Model.UndoScope(TextContent.addSpriteInfluence))
            {
                characterPart.bones = characterBones.ToArray();
                m_Events.characterPartChanged.Invoke(characterPart);
                
                UpdateSelectedBonesInfluencedSprites();
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                UpdateAddRemoveButtons();
            }
        }

        void RemoveSelectedSpriteInfluenceFromBone()
        {
            var character = m_Model.characterSkeleton;
            
            if (character == null)
                return;

            var selectedBone = m_Model.selectedBone;
            var selectedSprite = m_Model.selectedSprite;

            using (m_Model.UndoScope(TextContent.removeSpriteInfluence))
            {
                var characterPart = m_Model.GetSpriteCharacterPart(selectedSprite);
                var characterBones = characterPart.bones.ToList();
                characterBones.Remove(selectedBone);
                
                characterPart.bones = characterBones.ToArray();
                m_Events.characterPartChanged.Invoke(characterPart);
                
                selectedSprite.SmoothFill();
                
                UpdateSelectedBonesInfluencedSprites();
                m_Model.view.UpdateList(m_Model.influencedSprites);
                m_Model.view.UpdateSelection(new TransformCache[] {m_Model.selectedSprite});
                UpdateAddRemoveButtons();
            }
        }

        void SelectSprite(IEnumerable<object> selectedSprite)
        {
            using (m_Model.UndoScope(TextContent.spriteSelection))
            {
                var firstSelected = selectedSprite.FirstOrDefault();
                m_Model.selectedSprite = (SpriteCache)firstSelected;
                m_Events.selectedSpriteChanged.Invoke(m_Model.selectedSprite);
            }
        }

        void UpdateSelectedBonesInfluencedSprites()
        {
            var selectedBone = m_Model.selectedBone;

            if (selectedBone != null)
            {
                if (m_Model.hasCharacter)
                {
                    var spriteList = new List<TransformCache>(); 
            
                    foreach (var characterPartCache in selectedBone.skinningCache.character.parts)
                    {
                        if(characterPartCache.bones.Contains(selectedBone))
                            spriteList.Add(characterPartCache.sprite);
                    }

                    m_Model.influencedSprites = spriteList;
                }
                else
                {
                    var spriteList = new List<TransformCache>();
                    var sprites = selectedBone.skinningCache.GetSprites();
                    foreach (var sprite in sprites)
                    {
                        if(sprite.GetSkeleton().bones.Contains(selectedBone))
                            spriteList.Add(sprite);
                    }

                    m_Model.influencedSprites = spriteList;
                }
            }
            else
            {
                m_Model.influencedSprites = new List<TransformCache>();    
            }
        }
        
        public bool ShouldEnableAddButton()
        {
            if (IsCharacterMode())
            {
                var hasSelectedBone = m_Model.selectedBone != null;
                var selectedSprite = m_Model.selectedSprite;
                var hasSelectedSprite = selectedSprite != null;
                return hasSelectedBone && hasSelectedSprite && !m_Model.influencedSprites.Contains(selectedSprite);
            }
            return false;
        }

        void UpdateAddRemoveButtons()
        {
            m_Model.view.ToggleAddButton(ShouldEnableAddButton());
            m_Model.view.ToggleRemoveButton(IsCharacterMode());
        }
        
        public bool IsCharacterMode()
        {
            return m_Model.hasCharacter && m_Model.skinningMode == SkinningMode.Character;
        }
    }
    
    interface IBoneSpriteInfluenceToolModel
    {
        IInfluenceWindow view { get; }
        BoneCache selectedBone { get; }
        SpriteCache selectedSprite { get; set; }
        List<TransformCache> influencedSprites { get; set; }
        bool hasCharacter { get; }
        SkinningMode skinningMode { get; }
        SkeletonCache characterSkeleton { get; }
        UndoScope UndoScope(string description);
        CharacterPartCache GetSpriteCharacterPart(SpriteCache sprite);
    }
    
    class BoneSpriteInfluenceTool : BaseTool, IBoneSpriteInfluenceToolModel
    {
        BoneSpriteInfluenceToolController m_Controller;
        MeshPreviewBehaviour m_MeshPreviewBehaviour = new MeshPreviewBehaviour();
        InfluenceWindow m_View;
        
        public SkeletonTool skeletonTool { get; set; }

        public override IMeshPreviewBehaviour previewBehaviour => m_MeshPreviewBehaviour;

        internal override void OnCreate()
        {
            m_Controller = new BoneSpriteInfluenceToolController(this, skinningCache.events);
        }
        
        IInfluenceWindow IBoneSpriteInfluenceToolModel.view => m_View;

        BoneCache IBoneSpriteInfluenceToolModel.selectedBone => skinningCache.skeletonSelection.elements.FirstOrDefault();

        SpriteCache IBoneSpriteInfluenceToolModel.selectedSprite
        {
            get => skinningCache.selectedSprite;
            set => skinningCache.selectedSprite = value;
        }

        List<TransformCache> IBoneSpriteInfluenceToolModel.influencedSprites { get; set; }
        bool IBoneSpriteInfluenceToolModel.hasCharacter => skinningCache.hasCharacter;
        SkinningMode IBoneSpriteInfluenceToolModel.skinningMode => skinningCache.mode;
        SkeletonCache IBoneSpriteInfluenceToolModel.characterSkeleton => skinningCache.character != null ? skinningCache.character.skeleton : null;

        UndoScope IBoneSpriteInfluenceToolModel.UndoScope(string description)
        {
            return skinningCache.UndoScope(description);
        }

        public CharacterPartCache GetSpriteCharacterPart(SpriteCache sprite)
        {
            return sprite.GetCharacterPart();
        }

        protected override void OnActivate()
        {
            m_Controller.Activate();
            if(skeletonTool != null)
                skeletonTool.Activate();
        }

        protected override void OnDeactivate()
        {
            m_Controller.Deactivate();
            if(skeletonTool != null)
                skeletonTool.Deactivate();
        }

        public override void Initialize(LayoutOverlay layout)
        {
            if (m_View == null)
            {
                m_View = InfluenceWindow.CreateFromUxml();
                m_View.SetListReorderable(false);
                m_View.SetAllowMultiselect(false);
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
    }
}