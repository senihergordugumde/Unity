using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEditor.U2D.Layout;
using UnityEngine.U2D;

namespace UnityEditor.U2D.Animation
{
    internal interface ICopyToolStringStore
    {
        string stringStore
        {
            get;
            set;
        }
    }

    internal class SystemCopyBufferStringStore : ICopyToolStringStore
    {
        public string stringStore
        {
            get => EditorGUIUtility.systemCopyBuffer;
            set => EditorGUIUtility.systemCopyBuffer = value;
        }
    }

    internal class CopyTool : MeshToolWrapper
    {
        ICopyToolStringStore m_CopyToolStringStore;
        CopyToolView m_CopyToolView;
        bool m_HasValidCopyData = false;
        int m_LastCopyDataHash;

        public float pixelsPerUnit
        {
            private get;
            set;
        }

        public bool hasValidCopiedData
        {
            get
            {
                var hashCode = m_CopyToolStringStore.stringStore.GetHashCode();
                if (hashCode != m_LastCopyDataHash)
                {
                    m_HasValidCopyData = IsValidCopyData(m_CopyToolStringStore.stringStore);
                    m_LastCopyDataHash = hashCode;
                }
                return m_HasValidCopyData;
            }
        }

        public ICopyToolStringStore copyToolStringStore
        {
            set => m_CopyToolStringStore = value;
        }

        internal override void OnCreate()
        {
            m_CopyToolView = new CopyToolView();
            m_CopyToolView.onPasteActivated += OnPasteActivated;
            m_CopyToolStringStore = new SystemCopyBufferStringStore();
            disableMeshEditor = true;
        }

        public override void Initialize(LayoutOverlay layout)
        {
            m_CopyToolView.Initialize(layout);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            m_CopyToolView.Show(skinningCache.bonesReadOnly);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            m_CopyToolView.Hide();
        }

        void CopyMeshFromSpriteCache(SpriteCache sprite, SkinningCopySpriteData skinningSpriteData)
        {
            if (meshTool == null)
                return;

            meshTool.SetupSprite(sprite);
            skinningSpriteData.vertices = meshTool.mesh.vertices;
            skinningSpriteData.indices = meshTool.mesh.indices;
            skinningSpriteData.edges = meshTool.mesh.edges;
            skinningSpriteData.boneWeightGuids = new List<string>(meshTool.mesh.bones.Length);
            skinningSpriteData.boneWeightNames = new List<string>(meshTool.mesh.bones.Length);
            foreach (var bone in meshTool.mesh.bones)
            {
                skinningSpriteData.boneWeightGuids.Add(bone.guid);
                skinningSpriteData.boneWeightNames.Add(bone.name);
            }
        }

        public void OnCopyActivated()
        {
            SkinningCopyData skinningCopyData = null;
            var selectedSprite = skinningCache.selectedSprite;
            if (selectedSprite == null)
            {
                var sprites = skinningCache.GetSprites();
                if(!skinningCache.character || sprites.Length > 1)
                    skinningCopyData = CopyAll();
                else if(sprites.Length == 1)
                    skinningCopyData = CopySingle(sprites[0]);
            }
            else
            {
                skinningCopyData = CopySingle(selectedSprite);
            }

            if (skinningCopyData != null)
                m_CopyToolStringStore.stringStore = SkinningCopyUtility.SerializeSkinningCopyDataToString(skinningCopyData);
            skinningCache.events.copy.Invoke();
        }

        SkinningCopyData CopyAll()
        {
            var skinningCopyData = new SkinningCopyData();
            skinningCopyData.pixelsPerUnit = pixelsPerUnit;

            var sprites = skinningCache.GetSprites();
            foreach (var sprite in sprites)
            {
                var skinningSpriteData = new SkinningCopySpriteData();
                skinningSpriteData.spriteName = sprite.name;

                var skeleton = skinningCache.GetEffectiveSkeleton(sprite);
                if (skeleton != null && skeleton.BoneCount > 0)
                {
                    if (skinningCache.hasCharacter)
                    {
                        // Order doesn't matter for character bones
                        skinningSpriteData.spriteBones = skeleton.bones.ToSpriteBone(Matrix4x4.identity).Select(x => new SpriteBoneCopyData()
                        {
                            spriteBone = x,
                            order = -1
                        }).ToList();
                    }
                    else
                    {
                        skinningSpriteData.spriteBones = new List<SpriteBoneCopyData>();
                        var bones = skeleton.bones.FindRoots();
                        foreach (var bone in bones)
                            GetSpriteBoneDataRecursively(skinningSpriteData.spriteBones, bone, skeleton.bones.ToList());
                    }
                }
                if (meshTool != null)
                {
                    CopyMeshFromSpriteCache(sprite, skinningSpriteData);
                }
                skinningCopyData.copyData.Add(skinningSpriteData);
            }

            if (meshTool != null)
            {
                meshTool.SetupSprite(null);
            }

            return skinningCopyData;
        }

        SkinningCopyData CopySingle(SpriteCache sprite)
        {
            var skinningCopyData = new SkinningCopyData();
            skinningCopyData.pixelsPerUnit = pixelsPerUnit;

            // Mesh
            var skinningSpriteData = new SkinningCopySpriteData();
            skinningSpriteData.spriteName = sprite.name;
            skinningCopyData.copyData.Add(skinningSpriteData);

            CopyMeshFromSpriteCache(sprite, skinningSpriteData);

            // Bones
            var rootBones = new List<BoneCache>();
            BoneCache[] boneCache = null;
            if (skinningCache.hasCharacter)
            {
                var characterPart = skinningCache.GetCharacterPart(sprite);
                if (characterPart != null && characterPart.bones != null)
                {
                    boneCache = characterPart.bones;
                    var bones = characterPart.bones.FindRoots();
                    foreach (var bone in bones)
                        rootBones.Add(bone);
                }
            }
            else
            {
                var skeleton = skinningCache.GetEffectiveSkeleton(sprite);
                if (skeleton != null && skeleton.BoneCount > 0)
                {
                    boneCache = skeleton.bones;
                    var bones = boneCache.FindRoots();
                    foreach (var bone in bones)
                        rootBones.Add(bone);
                }
            }

            if (rootBones.Count > 0)
            {
                skinningSpriteData.spriteBones = new List<SpriteBoneCopyData>();
                foreach (var rootBone in rootBones)
                {
                    var rootBoneIndex = skinningSpriteData.spriteBones.Count;
                    GetSpriteBoneDataRecursively(skinningSpriteData.spriteBones, rootBone, boneCache.ToList());
                    if (skinningCache.hasCharacter)
                    {
                        // Offset the bones based on the currently selected Sprite in Character mode
                        var characterPart = sprite.GetCharacterPart();
                        if (characterPart != null)
                        {
                            var offset = characterPart.position;
                            var rootSpriteBone = skinningSpriteData.spriteBones[rootBoneIndex];
                            rootSpriteBone.spriteBone.position = rootSpriteBone.spriteBone.position - offset;
                            skinningSpriteData.spriteBones[rootBoneIndex] = rootSpriteBone;
                        }
                    }
                }
            }

            return skinningCopyData;
        }

        static void GetSpriteBoneDataRecursively(IList<SpriteBoneCopyData> bones, BoneCache rootBone, List<BoneCache> boneCache)
        {
            AppendSpriteBoneDataRecursively(bones, rootBone, -1, boneCache);
        }

        static void AppendSpriteBoneDataRecursively(IList<SpriteBoneCopyData> bones, BoneCache currentBone, int parentIndex, List<BoneCache> boneCache)
        {
            var currentParentIndex = bones.Count;

            var boneCopyData = new SpriteBoneCopyData()
            {
                spriteBone = new SpriteBone()
                {
                    name = currentBone.name,
                    guid = currentBone.guid,
                    color = currentBone.bindPoseColor,
                    parentId = parentIndex
                },
                order = boneCache.FindIndex(x => x == currentBone)
            };
            if (boneCopyData.order < 0)
            {
                boneCopyData.order = boneCache.Count;
                boneCache.Add(currentBone);
            }
                
            if (parentIndex == -1 && currentBone.parentBone != null)
            {
                boneCopyData.spriteBone.position = currentBone.position;
                boneCopyData.spriteBone.rotation = currentBone.rotation;
            }
            else
            {
                boneCopyData.spriteBone.position = currentBone.localPosition;
                boneCopyData.spriteBone.rotation = currentBone.localRotation;
            }
            boneCopyData.spriteBone.position = new Vector3(boneCopyData.spriteBone.position.x, boneCopyData.spriteBone.position.y, currentBone.depth);

            boneCopyData.spriteBone.length = currentBone.localLength;
            bones.Add(boneCopyData);
            foreach (var child in currentBone)
            {
                var childBone = child as BoneCache;
                if (childBone != null)
                    AppendSpriteBoneDataRecursively(bones, childBone, currentParentIndex, boneCache);
            }
        }

        public void OnPasteActivated(bool shouldPasteBones, bool shouldPasteMesh, bool shouldFlipX, bool shouldFlipY)
        {
            var copyBuffer = m_CopyToolStringStore.stringStore;
            if (!IsValidCopyData(copyBuffer))
            {
                Debug.LogError(TextContent.copyError1);
                return;
            }

            var skinningCopyData = SkinningCopyUtility.DeserializeStringToSkinningCopyData(copyBuffer);
            if (skinningCopyData == null || skinningCopyData.copyData.Count == 0)
            {
                Debug.LogError(TextContent.copyError2);
                return;
            }

            var scale = 1f;
            if (skinningCopyData.pixelsPerUnit > 0f)
                scale = pixelsPerUnit / skinningCopyData.pixelsPerUnit;

            var sprites = skinningCache.GetSprites();
            var doesCopyContainMultipleSprites = skinningCopyData.copyData.Count > 1;
            if (doesCopyContainMultipleSprites && skinningCopyData.copyData.Count != sprites.Length && shouldPasteMesh)
            {
                Debug.LogError(string.Format(TextContent.copyError3, sprites.Length, skinningCopyData.copyData.Count));
                return;
            }

            using (skinningCache.UndoScope(TextContent.pasteData))
            {
                BoneCache[] boneStorage = null;
                if (shouldPasteBones && doesCopyContainMultipleSprites && skinningCache.hasCharacter)
                {
                    var skinningSpriteData = skinningCopyData.copyData[0];
                    boneStorage = skinningCache.CreateBoneCacheFromSpriteBones(skinningSpriteData.spriteBones.Select(y => y.spriteBone).ToArray(), scale);
                    if (shouldFlipX || shouldFlipY)
                    {
                        var characterRect = new Rect(Vector2.zero, skinningCache.character.dimension);
                        var newPositions = new Vector3[boneStorage.Length];
                        var newRotations = new Quaternion[boneStorage.Length];
                        for (var i = 0; i < boneStorage.Length; ++i)
                        {
                            newPositions[i] = GetFlippedBonePosition(boneStorage[i], Vector2.zero, characterRect, shouldFlipX, shouldFlipY);
                            newRotations[i] = GetFlippedBoneRotation(boneStorage[i], shouldFlipX, shouldFlipY);
                        }
                        for (var i = 0; i < boneStorage.Length; ++i)
                        {
                            boneStorage[i].position = newPositions[i];
                            boneStorage[i].rotation = newRotations[i];
                        }
                    }
                    
                    var skeleton = skinningCache.character.skeleton;
                    skeleton.SetBones(boneStorage);
                    skinningCache.events.skeletonTopologyChanged.Invoke(skeleton);
                }

                foreach (var copySpriteData in skinningCopyData.copyData)
                {
                    SpriteCache sprite = null;
                    if (skinningCache.selectedSprite != null && skinningCopyData.copyData.Count == 1)
                    {
                        sprite = skinningCache.selectedSprite;
                    }
                    if (sprite == null && !string.IsNullOrEmpty(copySpriteData.spriteName))
                    {
                        sprite = sprites.FirstOrDefault(x => x.name == copySpriteData.spriteName);
                    }
                    
                    if (sprite == null)
                        continue;

                    if (shouldPasteBones && (!skinningCache.hasCharacter || !doesCopyContainMultipleSprites))
                    {
                        var newBones = new SpriteBone[copySpriteData.spriteBones.Count];
                        for (var i = 0; i < copySpriteData.spriteBones.Count; ++i)
                        {
                            var order = copySpriteData.spriteBones[i].order;
                            newBones[order] = copySpriteData.spriteBones[i].spriteBone;
                            var parentId = newBones[order].parentId;
                            if (parentId >= 0)
                            {
                                newBones[order].parentId = copySpriteData.spriteBones[parentId].order;
                            }
                        }
                        boneStorage = PasteBonesInSprite(sprite, newBones, shouldFlipX, shouldFlipY, scale);
                    }

                    if (shouldPasteMesh && meshTool != null)
                    {
                        PasteMeshInSprite(sprite, copySpriteData, shouldFlipX, shouldFlipY, scale);
                    }
                }

                if (boneStorage != null)
                {
                    skinningCache.skeletonSelection.elements = boneStorage;
                    skinningCache.events.boneSelectionChanged.Invoke();
                }
            }
            skinningCache.events.paste.Invoke(shouldPasteBones, shouldPasteMesh, shouldFlipX, shouldFlipY);
        }
        
        static bool IsValidCopyData(string copyBuffer)
        {
            return SkinningCopyUtility.CanDeserializeStringToSkinningCopyData(copyBuffer);
        }

        static Vector3 GetFlippedBonePosition(BoneCache bone, Vector2 startPosition, Rect spriteRect, bool flipX, bool flipY)
        {
            Vector3 position = startPosition;
            if (flipX)
                position.x += spriteRect.width - bone.position.x;
            else
                position.x += bone.position.x;

            if (flipY)
                position.y += spriteRect.height - bone.position.y;
            else
                position.y += bone.position.y;

            position.z = bone.position.z;
            return position;
        }

        static Quaternion GetFlippedBoneRotation(BoneCache bone, bool flipX, bool flipY)
        {
            var euler = bone.rotation.eulerAngles;
            if (flipX)
            {
                if (euler.z <= 180)
                    euler.z = 180 - euler.z;
                else
                    euler.z = 540 - euler.z;
            }
            if (flipY)
                euler.z = 360 - euler.z;
            return Quaternion.Euler(euler);
        }

        static void SetBonePositionAndRotation(BoneCache[] boneCache, TransformCache bone, Vector3[] position, Quaternion[] rotation)
        {
            var index = Array.FindIndex(boneCache, x => x == bone);
            if (index >= 0)
            {
                bone.position = position[index];
                bone.rotation = rotation[index];
            }
            foreach (var child in bone.children)
            {
                SetBonePositionAndRotation(boneCache, child, position, rotation);
            }
        }
        
        BoneCache[] PasteBonesInSprite(SpriteCache sprite, SpriteBone[] newBones, bool shouldFlipX, bool shouldFlipY, float scale)
        {
            var newBonesStore = skinningCache.CreateBoneCacheFromSpriteBones(newBones, scale);
            if (newBonesStore.Length == 0)
                return null;

            if (sprite == null || (skinningCache.mode == SkinningMode.SpriteSheet && skinningCache.hasCharacter))
                return null;

            var spriteRect = sprite.textureRect;
            var skeleton = skinningCache.GetEffectiveSkeleton(sprite);

            var rectPosition = spriteRect.position;
            if (skinningCache.mode == SkinningMode.Character)
            {
                var characterPart = sprite.GetCharacterPart();
                if (characterPart == null)
                    return null;
                rectPosition = characterPart.position;
            }

            var newPositions = new Vector3[newBonesStore.Length];
            var newRotations = new Quaternion[newBonesStore.Length];
            for (var i = 0; i < newBonesStore.Length; ++i)
            {
                newPositions[i] = GetFlippedBonePosition(newBonesStore[i], rectPosition, spriteRect, shouldFlipX, shouldFlipY);
                newRotations[i] = GetFlippedBoneRotation(newBonesStore[i], shouldFlipX, shouldFlipY);
            }
            foreach (var bone in newBonesStore)
            {
                if(bone.parent == null)
                    SetBonePositionAndRotation(newBonesStore, bone, newPositions, newRotations);
            }

            if (skinningCache.mode == SkinningMode.SpriteSheet)
            {
                skeleton.SetBones(newBonesStore);
            }
            else
            {
                var existingBones = skeleton.bones;
                var existingBoneNames = skeleton.bones.Select(x => x.name).ToList();
                foreach (var newBone in newBonesStore)
                {
                    var index = Array.FindIndex(existingBones, x => x.guid == newBone.guid);
                    if (index >= 0)
                        skeleton.DestroyBone(existingBones[index]);
                    else
                    {
                        if (existingBoneNames.Contains(newBone.name))
                        {
                            newBone.name = SkeletonController.AutoBoneName(newBone.parentBone, existingBones);
                            existingBoneNames.Add(newBone.name);
                        }                        
                    }
                    
                    skeleton.AddBone(newBone);
                }

                skeleton.SetDefaultPose();
            }

            skinningCache.events.skeletonTopologyChanged.Invoke(skeleton);
            return newBonesStore;
        }

        void PasteMeshInSprite(SpriteCache sprite, SkinningCopySpriteData copySpriteData, bool shouldFlipX, bool shouldFlipY, float scale)
        {
            if (sprite == null)
                return;

            meshTool.SetupSprite(sprite);
            meshTool.mesh.vertices = copySpriteData.vertices;
            if (!Mathf.Approximately(scale, 1f) || shouldFlipX || shouldFlipY)
            {
                var spriteRect = sprite.textureRect;
                foreach (var vertex in meshTool.mesh.vertices)
                {
                    var position = vertex.position;
                    if (!Mathf.Approximately(scale, 1f))
                        position *= scale;
                    if (shouldFlipX)
                        position.x = spriteRect.width - vertex.position.x;
                    if (shouldFlipY)
                        position.y = spriteRect.height - vertex.position.y;
                    vertex.position = position;
                }
            }
            meshTool.mesh.indices = copySpriteData.indices;
            meshTool.mesh.edges = copySpriteData.edges;

            var boneIndices = new int[copySpriteData.boneWeightGuids.Count];
            BoneCache[] newBones = null;

            var skeleton = skinningCache.GetEffectiveSkeleton(sprite);
            var hasGuids = copySpriteData.boneWeightGuids.Count > 0 && !string.IsNullOrEmpty(copySpriteData.boneWeightGuids[0]);
            if (hasGuids)
            {
                var boneList = new List<BoneCache>();
                boneIndices = new int[copySpriteData.boneWeightGuids.Count];
                var index = 0;
                for (var i = 0; i < copySpriteData.boneWeightGuids.Count; ++i)
                {
                    var boneGuid = copySpriteData.boneWeightGuids[i];
                    var bone = skeleton.bones.FirstOrDefault(bone => bone.guid == boneGuid);
                    boneIndices[i] = -1;
                    
                    if (bone == null)
                        continue;

                    var pastedBone = copySpriteData.spriteBones.FirstOrDefault(x => x.spriteBone.guid == boneGuid);
                    if (pastedBone != null)
                    {
                        boneIndices[i] = index++;
                        boneList.Add(bone);                        
                    }
                }
                newBones = boneList.ToArray();           
            }
            else
            {
                // Attempt to link weights based on existing bone names
                var characterBones = new List<BoneCache>();
                for (var i = 0; i < copySpriteData.boneWeightNames.Count; ++i)
                {
                    boneIndices[i] = -1;
                    var boneName = copySpriteData.boneWeightNames[i];
                    for (var j = 0; j < skeleton.bones.Length; ++j)
                    {
                        if (skeleton.bones[j].name == boneName)
                        {
                            boneIndices[i] = characterBones.Count;
                            characterBones.Add(skeleton.bones[j]);
                            break;
                        }
                    }
                }
                newBones = characterBones.ToArray();
            }

            // Remap new bone indexes from copied bone indexes
            foreach (var vertex in meshTool.mesh.vertices)
            {
                var editableBoneWeight = vertex.editableBoneWeight;

                for (var i = 0; i < editableBoneWeight.Count; ++i)
                {
                    if (!editableBoneWeight[i].enabled)
                        continue;

                    if (boneIndices.Length > editableBoneWeight[i].boneIndex)
                    {
                        var boneIndex = boneIndices[editableBoneWeight[i].boneIndex];
                        if (boneIndex != -1)
                            editableBoneWeight[i].boneIndex = boneIndex;                        
                    }
                }
            }

            // Update associated bones for mesh
            meshTool.mesh.SetCompatibleBoneSet(newBones);
            meshTool.mesh.bones = newBones; // Fixes weights for bones that do not exist                

            // Update associated bones for character
            if (skinningCache.hasCharacter)
            {
                var characterPart = sprite.GetCharacterPart();
                if (characterPart != null)
                {
                    characterPart.bones = newBones;
                    skinningCache.events.characterPartChanged.Invoke(characterPart);
                }
            }

            meshTool.UpdateMesh();
        }
    }

    internal class CopyToolView
    {
        PastePanel m_PastePanel;

        public event Action<bool, bool, bool, bool> onPasteActivated = (bone, mesh, flipX, flipY) => {};

        public void Show(bool readonlyBone)
        {
            m_PastePanel.SetHiddenFromLayout(false);
            m_PastePanel.BonePasteEnable(!readonlyBone);
        }

        public void Hide()
        {
            m_PastePanel.SetHiddenFromLayout(true);
        }

        public void Initialize(LayoutOverlay layoutOverlay)
        {
            m_PastePanel = PastePanel.GenerateFromUXML();
            BindElements();
            layoutOverlay.rightOverlay.Add(m_PastePanel);
            m_PastePanel.SetHiddenFromLayout(true);
        }

        void BindElements()
        {
            m_PastePanel.onPasteActivated += OnPasteActivated;
        }

        void OnPasteActivated(bool bone, bool mesh, bool flipX, bool flipY)
        {
            onPasteActivated(bone, mesh, flipX, flipY);
        }
    }
}
