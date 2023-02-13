#if ENABLE_ANIMATION_COLLECTION && ENABLE_ANIMATION_BURST
#define ENABLE_ANIMATION_PERFORMANCE
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using UnityEditor.U2D.Sprites;
using UnityEngine.U2D.Animation;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace UnityEditor.U2D.Animation
{
    internal class SpritePostProcess : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            var ai = GetSpriteEditorDataProvider(assetPath);
            var characterDataProvider = ai?.GetDataProvider<ICharacterDataProvider>();

            if (characterDataProvider != null)
            {
                var mainSkeletonBonesDataProvider = ai?.GetDataProvider<IMainSkeletonDataProvider>();
                if (mainSkeletonBonesDataProvider != null)
                {
                    var skinningCache = Cache.Create<SkinningCache>();
                    skinningCache.Create(ai, new SkinningCachePersistentStateTemp());

                    var skeletonBones = mainSkeletonBonesDataProvider.GetMainSkeletonData().bones ?? new SpriteBone[0];
                    RemapCharacterPartsToNewBones(skinningCache, skeletonBones);

                    SkinningModule.ApplyChanges(skinningCache, ai);
                }
            }
        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            var ai = GetSpriteEditorDataProvider(assetPath);
            if (ai != null)
            {
                var definitionScale = CalculateDefinitionScale(texture, ai.GetDataProvider<ITextureDataProvider>());
                ai.InitSpriteEditorDataProvider();
                PostProcessBoneData(ai, definitionScale, sprites);
                PostProcessSpriteMeshData(ai, definitionScale, sprites);
                BoneGizmo.instance.ClearSpriteBoneCache();
            }

            // Get all SpriteSkin in scene and inform them to refresh their cache
            RefreshSpriteSkinCache();
        }

        static void RemapCharacterPartsToNewBones(SkinningCache skinningCache, SpriteBone[] newBones)
        {
            var skeleton = skinningCache.character.skeleton;
            var previousStateBones = skeleton.bones;
            var skeletonBones = skinningCache.CreateBoneCacheFromSpriteBones(newBones, 1.0f);
            skeleton.SetBones(skeletonBones);

            for (var i = 0; i < skinningCache.character.parts.Length; i++)
            {
                var characterPart = skinningCache.character.parts[i];
                var useGuids = !skeletonBones.All(newBone => previousStateBones.All(oldBone => newBone.guid != oldBone.guid));
                characterPart.bones = useGuids ?
                    characterPart.bones.Select(partBone => Array.Find(skeletonBones, skeletonBone => partBone.guid == skeletonBone.guid)).ToArray() : 
                    characterPart.bones.Select(partBone => skeletonBones.ElementAtOrDefault(Array.FindIndex(previousStateBones, oldBone => partBone.guid == oldBone.guid))).ToArray();

                var mesh = skinningCache.GetMesh(characterPart.sprite);
                 if (mesh != null)
                     mesh.SetCompatibleBoneSet(characterPart.bones);

                skinningCache.character.parts[i] = characterPart;
            }
        }

        static void RefreshSpriteSkinCache()
        {
#if ENABLE_ANIMATION_PERFORMANCE
            var spriteSkins = GameObject.FindObjectsOfType<SpriteSkin>();
            foreach (var ss in spriteSkins)
            {
                ss.ResetSprite();
            }
#endif
        }

        static void CalculateLocaltoWorldMatrix(int i, SpriteRect spriteRect, float definitionScale, float pixelsPerUnit, List<UnityEngine.U2D.SpriteBone> spriteBone, ref UnityEngine.U2D.SpriteBone?[] outpriteBone, ref NativeArray<Matrix4x4> bindPose)
        {
            if (outpriteBone[i] != null)
                return;
            UnityEngine.U2D.SpriteBone sp = spriteBone[i];
            var isRoot = sp.parentId == -1;
            var position = isRoot ? (spriteBone[i].position - Vector3.Scale(spriteRect.rect.size, spriteRect.pivot)) : spriteBone[i].position;
            position.z = 0f;
            sp.position = position * definitionScale / pixelsPerUnit;
            sp.length = spriteBone[i].length * definitionScale / pixelsPerUnit;
            outpriteBone[i] = sp;

            // Calculate bind poses
            var worldPosition = Vector3.zero;
            var worldRotation = Quaternion.identity;

            if (sp.parentId == -1)
            {
                worldPosition = sp.position;
                worldRotation = sp.rotation;
            }
            else
            {
                if (outpriteBone[sp.parentId] == null)
                {
                    CalculateLocaltoWorldMatrix(sp.parentId, spriteRect, definitionScale, pixelsPerUnit, spriteBone, ref outpriteBone, ref bindPose);
                }
                var parentBindPose = bindPose[sp.parentId];
                var invParentBindPose = Matrix4x4.Inverse(parentBindPose);

                worldPosition = invParentBindPose.MultiplyPoint(sp.position);
                worldRotation = sp.rotation * invParentBindPose.rotation;
            }

            // Practically Matrix4x4.SetTRInverse
            var rot = Quaternion.Inverse(worldRotation);
            Matrix4x4 mat = Matrix4x4.identity;
            mat = Matrix4x4.Rotate(rot);
            mat = mat * Matrix4x4.Translate(-worldPosition);


            bindPose[i] = mat;
        }

        static bool PostProcessBoneData(ISpriteEditorDataProvider spriteDataProvider, float definitionScale, Sprite[] sprites)
        {
            var boneDataProvider = spriteDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            var textureDataProvider = spriteDataProvider.GetDataProvider<ITextureDataProvider>();

            if (sprites == null || sprites.Length == 0 || boneDataProvider == null || textureDataProvider == null)
                return false;

            var dataChanged = false;
            var spriteRects = spriteDataProvider.GetSpriteRects();
            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                {
                    var spriteBone = boneDataProvider.GetBones(guid);
                    if (spriteBone == null)
                        continue;

                    var spriteBoneCount = spriteBone.Count;
                    if (spriteBoneCount == 0)
                        continue;

                    var spriteRect = spriteRects.First(s => { return s.spriteID == guid; });

                    var bindPose = new NativeArray<Matrix4x4>(spriteBoneCount, Allocator.Temp);
                    var outputSpriteBones = new UnityEngine.U2D.SpriteBone ? [spriteBoneCount];
                    for (int i = 0; i < spriteBoneCount; ++i)
                    {
                        CalculateLocaltoWorldMatrix(i, spriteRect, definitionScale, sprite.pixelsPerUnit, spriteBone, ref outputSpriteBones, ref bindPose);
                    }
                    sprite.SetBindPoses(bindPose);
                    sprite.SetBones(outputSpriteBones.Select(x => x.Value).ToArray());
                    bindPose.Dispose();

                    dataChanged = true;
                }
            }

            return dataChanged;
        }

        static bool PostProcessSpriteMeshData(ISpriteEditorDataProvider spriteDataProvider, float definitionScale, Sprite[] sprites)
        {
            var spriteMeshDataProvider = spriteDataProvider.GetDataProvider<ISpriteMeshDataProvider>();
            var boneDataProvider = spriteDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            var textureDataProvider = spriteDataProvider.GetDataProvider<ITextureDataProvider>();
            if (sprites == null || sprites.Length == 0 || spriteMeshDataProvider == null || textureDataProvider == null)
                return false;

            var dataChanged = false;
            var spriteRects = spriteDataProvider.GetSpriteRects();
            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                var vertices = spriteMeshDataProvider.GetVertices(guid);
                int[] indices = null;
                if (vertices.Length > 2)
                    indices = spriteMeshDataProvider.GetIndices(guid);

                if (indices != null && indices.Length > 2 && vertices.Length > 2 )
                {
                    var spriteRect = spriteRects.First(s => { return s.spriteID == guid; });
                    var spriteBone = boneDataProvider.GetBones(guid);
                    var hasBones = spriteBone != null && spriteBone.Count > 0;
                    var hasInvalidWeights = false;

                    var vertexArray = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
                    var boneWeightArray = new NativeArray<BoneWeight>(vertices.Length, Allocator.Temp);

                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        var boneWeight = vertices[i].boneWeight;

                        vertexArray[i] = (Vector3)(vertices[i].position - Vector2.Scale(spriteRect.rect.size, spriteRect.pivot)) * definitionScale / sprite.pixelsPerUnit;
                        boneWeightArray[i] = boneWeight;

                        if (hasBones && !hasInvalidWeights)
                        {
                            var sum = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;
                            hasInvalidWeights = sum < 0.999f;
                        }
                    }

                    var indicesArray = new NativeArray<ushort>(indices.Length, Allocator.Temp);
                    for (int i = 0; i < indices.Length; ++i)
                        indicesArray[i] = (ushort)indices[i];

                    sprite.SetVertexCount(vertices.Length);
                    sprite.SetVertexAttribute<Vector3>(VertexAttribute.Position, vertexArray);
                    sprite.SetIndices(indicesArray);
                    sprite.SetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight, boneWeightArray);
                    vertexArray.Dispose();
                    boneWeightArray.Dispose();
                    indicesArray.Dispose();

                    // Deformed Sprites require proper Tangent Channels if Lit. Enable Tangent channels.
                    if (hasBones)
                    {
                        var tangentArray = new NativeArray<Vector4>(vertices.Length, Allocator.Temp);
                        for (int i = 0; i < vertices.Length; ++i)
                            tangentArray[i] = new Vector4(1.0f, 0.0f, 0, -1.0f);
                        sprite.SetVertexAttribute<Vector4>(VertexAttribute.Tangent, tangentArray);
                        tangentArray.Dispose();
                    }

                    dataChanged = true;

                    if (hasBones && hasInvalidWeights)
                        Debug.LogWarning("Sprite \"" + spriteRect.name + "\" contains bone weights which sum zero or are not normalized. To avoid visual artifacts please consider fixing them.");
                }
                else
                {
                    var boneWeightArray = new NativeArray<BoneWeight>(sprite.GetVertexCount(), Allocator.Temp);
                    var defaultBoneWeight = new BoneWeight() { weight0 = 1f };

                    for (var i = 0; i < boneWeightArray.Length; ++i)
                        boneWeightArray[i] = defaultBoneWeight;

                    sprite.SetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight, boneWeightArray);
                }
            }

            return dataChanged;
        }

        static float CalculateDefinitionScale(Texture2D texture, ITextureDataProvider dataProvider)
        {
            float definitionScale = 1;
            if (texture != null && dataProvider != null)
            {
                int actualWidth = 0, actualHeight = 0;
                dataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
                float definitionScaleW = texture.width / (float)actualWidth;
                float definitionScaleH = texture.height / (float)actualHeight;
                definitionScale = Mathf.Min(definitionScaleW, definitionScaleH);
            }
            return definitionScale;
        }

        static ISpriteEditorDataProvider GetSpriteEditorDataProvider(string assetPath)
        {
            var dataProviderFactories = new SpriteDataProviderFactories();
            dataProviderFactories.Init();
            return dataProviderFactories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(assetPath));
        }
        
        internal class SkinningCachePersistentStateTemp : ISkinningCachePersistentState
        {
            private string _lastSpriteId;
            private Tools _lastUsedTool;
            private List<int> _lastBoneSelectionIds = null;
            private Texture2D _lastTexture = null;
            private SerializableDictionary<int, BonePose> _lastPreviewPose = null;
            private SerializableDictionary<int, bool> _lastBoneVisibility = null;
            private SerializableDictionary<int, bool> _lastBoneExpansion = null;
            private SerializableDictionary<string, bool> _lastSpriteVisibility = null;
            private SerializableDictionary<int, bool> _lastGroupVisibility = null;
            private SkinningMode _lastMode;
            private bool _lastVisibilityToolActive;
            private int _lastVisibilityToolIndex;
            private IndexedSelection _lastVertexSelection = null;
            private float _lastBrushSize;
            private float _lastBrushHardness;
            private float _lastBrushStep;

            string ISkinningCachePersistentState.lastSpriteId
            {
                get => _lastSpriteId;
                set => _lastSpriteId = value;
            }

            Tools ISkinningCachePersistentState.lastUsedTool
            {
                get => _lastUsedTool;
                set => _lastUsedTool = value;
            }

            List<int> ISkinningCachePersistentState.lastBoneSelectionIds => _lastBoneSelectionIds;

            Texture2D ISkinningCachePersistentState.lastTexture
            {
                get => _lastTexture;
                set => _lastTexture = value;
            }

            SerializableDictionary<int, BonePose> ISkinningCachePersistentState.lastPreviewPose => _lastPreviewPose;

            SerializableDictionary<int, bool> ISkinningCachePersistentState.lastBoneVisibility => _lastBoneVisibility;

            SerializableDictionary<int, bool> ISkinningCachePersistentState.lastBoneExpansion => _lastBoneExpansion;

            SerializableDictionary<string, bool> ISkinningCachePersistentState.lastSpriteVisibility => _lastSpriteVisibility;

            SerializableDictionary<int, bool> ISkinningCachePersistentState.lastGroupVisibility => _lastGroupVisibility;

            SkinningMode ISkinningCachePersistentState.lastMode
            {
                get => _lastMode;
                set => _lastMode = value;
            }

            bool ISkinningCachePersistentState.lastVisibilityToolActive
            {
                get => _lastVisibilityToolActive;
                set => _lastVisibilityToolActive = value;
            }

            int ISkinningCachePersistentState.lastVisibilityToolIndex
            {
                get => _lastVisibilityToolIndex;
                set => _lastVisibilityToolIndex = value;
            }

            IndexedSelection ISkinningCachePersistentState.lastVertexSelection => _lastVertexSelection;

            float ISkinningCachePersistentState.lastBrushSize
            {
                get => _lastBrushSize;
                set => _lastBrushSize = value;
            }

            float ISkinningCachePersistentState.lastBrushHardness
            {
                get => _lastBrushHardness;
                set => _lastBrushHardness = value;
            }

            float ISkinningCachePersistentState.lastBrushStep
            {
                get => _lastBrushStep;
                set => _lastBrushStep = value;
            }
        }
    }
}
