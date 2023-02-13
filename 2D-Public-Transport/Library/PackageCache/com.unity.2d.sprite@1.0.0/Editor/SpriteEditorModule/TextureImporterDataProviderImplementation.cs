using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.U2D.Sprites
{
    internal class SpriteDataProviderBase
    {
        public SpriteDataProviderBase(TextureImporterDataProvider dp)
        {
            dataProvider = dp;
        }

        protected TextureImporterDataProvider dataProvider { get; private set; }
    }

    internal class SpriteBoneDataTransfer : SpriteDataProviderBase, ISpriteBoneDataProvider
    {
        public SpriteBoneDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        public List<SpriteBone> GetBones(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetBones(GUID guid, List<SpriteBone> bones)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spriteBone = bones;
        }

        private static List<SpriteBone> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var sp = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_Bones") :
                importer.FindProperty("m_SpriteSheet.m_Bones");

            var spriteBone = new List<SpriteBone>(sp.arraySize);
            if (sp.arraySize > 0)
            {
                var boneSO = sp.GetArrayElementAtIndex(0);
                for (int i = 0; i < sp.arraySize; ++i, boneSO.Next(false))
                {
                    var sb = new SpriteBone();
                    sb.length = boneSO.FindPropertyRelative("length").floatValue;
                    sb.position = boneSO.FindPropertyRelative("position").vector3Value;
                    sb.rotation = boneSO.FindPropertyRelative("rotation").quaternionValue;
                    sb.parentId = boneSO.FindPropertyRelative("parentId").intValue;
                    sb.name = boneSO.FindPropertyRelative("name").stringValue;
                    sb.guid = boneSO.FindPropertyRelative("guid").stringValue;
                    sb.color = boneSO.FindPropertyRelative("color").colorValue;
                    spriteBone.Add(sb);
                }
            }
            return spriteBone;
        }

        public static void Apply(SerializedProperty rectSP, List<SpriteBone> spriteBone)
        {
            var sp = rectSP.FindPropertyRelative("m_Bones");
            sp.arraySize = spriteBone.Count;
            if (spriteBone.Count > 0)
            {
                var boneSO = sp.GetArrayElementAtIndex(0);
                for (int i = 0; i < sp.arraySize; ++i, boneSO.Next(false))
                {
                    var sb = spriteBone[i];
                    boneSO.FindPropertyRelative("length").floatValue = sb.length;
                    boneSO.FindPropertyRelative("position").vector3Value = sb.position;
                    boneSO.FindPropertyRelative("rotation").quaternionValue = sb.rotation;
                    boneSO.FindPropertyRelative("parentId").intValue = sb.parentId;
                    boneSO.FindPropertyRelative("name").stringValue = sb.name;
                    boneSO.FindPropertyRelative("guid").stringValue = sb.guid;
                    boneSO.FindPropertyRelative("color").colorValue = sb.color;
                }
            }
        }
    }

    internal class SpriteOutlineDataTransfer : SpriteDataProviderBase, ISpriteOutlineDataProvider
    {
        public SpriteOutlineDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spriteOutline = data;
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail = value;
        }

        private static List<Vector2[]> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var outlineSP = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_Outline") :
                importer.FindProperty("m_SpriteSheet.m_Outline");

            var outline = new List<Vector2[]>();
            if (outlineSP.arraySize > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < outlineSP.arraySize; ++j, outlinePathSP.Next(false))
                {
                    var o = new Vector2[outlinePathSP.arraySize];
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < outlinePathSP.arraySize; ++k, psp.Next(false))
                        {
                            o[k] = psp.vector2Value;
                        }
                    }
                    outline.Add(o);
                }
            }
            return outline;
        }

        public static void Apply(SerializedProperty rectSP, List<Vector2[]> outline)
        {
            var outlineSP = rectSP.FindPropertyRelative("m_Outline");
            outlineSP.arraySize = outline.Count;
            if (outline.Count > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < outline.Count; ++j, outlinePathSP.Next(false))
                {
                    var o = outline[j];
                    outlinePathSP.arraySize = o.Length;
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < o.Length; ++k, psp.Next(false))
                        {
                            psp.vector2Value = o[k];
                        }
                    }
                }
            }
        }
    }

    internal class SpriteMeshDataTransfer : SpriteDataProviderBase, ISpriteMeshDataProvider
    {
        public SpriteMeshDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        public Vertex2DMetaData[] GetVertices(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadVertex2DMetaData(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetVertices(GUID guid, Vertex2DMetaData[] data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).vertices = new List<Vertex2DMetaData>(data);
        }

        public int[] GetIndices(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadIndices(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetIndices(GUID guid, int[] indices)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).indices = new List<int>(indices);
        }

        public Vector2Int[] GetEdges(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadEdges(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetEdges(GUID guid, Vector2Int[] edges)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).edges = new List<Vector2Int>(edges);
        }

        private Vertex2DMetaData[] LoadVertex2DMetaData(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var verticesSP = so.FindPropertyRelative("m_Vertices");
            var vertices = new Vertex2DMetaData[verticesSP.arraySize];
            if (verticesSP.arraySize > 0)
            {
                var weightsSP = so.FindPropertyRelative("m_Weights");
                var vsp = verticesSP.GetArrayElementAtIndex(0);
                var wsp = weightsSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < verticesSP.arraySize; ++i, vsp.Next(false), wsp.Next(false))
                {
                    vertices[i] = new Vertex2DMetaData
                    {
                        position = vsp.vector2Value,
                        boneWeight = new BoneWeight
                        {
                            weight0 = wsp.FindPropertyRelative("weight[0]").floatValue,
                            weight1 = wsp.FindPropertyRelative("weight[1]").floatValue,
                            weight2 = wsp.FindPropertyRelative("weight[2]").floatValue,
                            weight3 = wsp.FindPropertyRelative("weight[3]").floatValue,
                            boneIndex0 = wsp.FindPropertyRelative("boneIndex[0]").intValue,
                            boneIndex1 = wsp.FindPropertyRelative("boneIndex[1]").intValue,
                            boneIndex2 = wsp.FindPropertyRelative("boneIndex[2]").intValue,
                            boneIndex3 = wsp.FindPropertyRelative("boneIndex[3]").intValue
                        }
                    };
                }
            }
            return vertices;
        }

        private int[] LoadIndices(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var indicesSP = so.FindPropertyRelative("m_Indices");
            var indices = new int[indicesSP.arraySize];
            if (indices.Length > 0)
            {
                var isp = indicesSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < indicesSP.arraySize; ++i, isp.Next(false))
                {
                    indices[i] = isp.intValue;
                }
            }

            return indices;
        }

        private Vector2Int[] LoadEdges(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var edgesSP = so.FindPropertyRelative("m_Edges");
            var edges = new Vector2Int[edgesSP.arraySize];
            if (edges.Length > 0)
            {
                var esp = edgesSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < edgesSP.arraySize; ++i, esp.Next(false))
                {
                    edges[i] = esp.vector2IntValue;
                }
            }

            return edges;
        }

        public static void Apply(SerializedProperty rectSP, List<Vertex2DMetaData> vertices, List<int> indices, List<Vector2Int> edges)
        {
            var verticesSP = rectSP.FindPropertyRelative("m_Vertices");
            var weightsSP = rectSP.FindPropertyRelative("m_Weights");
            var indicesSP = rectSP.FindPropertyRelative("m_Indices");
            var edgesSP = rectSP.FindPropertyRelative("m_Edges");

            verticesSP.arraySize = vertices.Count;
            weightsSP.arraySize = vertices.Count;
            if (vertices.Count > 0)
            {
                var vsp = verticesSP.GetArrayElementAtIndex(0);
                var wsp = weightsSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < vertices.Count; ++i, vsp.Next(false), wsp.Next(false))
                {
                    vsp.vector2Value = vertices[i].position;
                    wsp.FindPropertyRelative("weight[0]").floatValue = vertices[i].boneWeight.weight0;
                    wsp.FindPropertyRelative("weight[1]").floatValue = vertices[i].boneWeight.weight1;
                    wsp.FindPropertyRelative("weight[2]").floatValue = vertices[i].boneWeight.weight2;
                    wsp.FindPropertyRelative("weight[3]").floatValue = vertices[i].boneWeight.weight3;
                    wsp.FindPropertyRelative("boneIndex[0]").intValue = vertices[i].boneWeight.boneIndex0;
                    wsp.FindPropertyRelative("boneIndex[1]").intValue = vertices[i].boneWeight.boneIndex1;
                    wsp.FindPropertyRelative("boneIndex[2]").intValue = vertices[i].boneWeight.boneIndex2;
                    wsp.FindPropertyRelative("boneIndex[3]").intValue = vertices[i].boneWeight.boneIndex3;
                }
            }

            indicesSP.arraySize = indices.Count;
            if (indices.Count > 0)
            {
                var isp = indicesSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < indices.Count; ++i, isp.Next(false))
                {
                    isp.intValue = indices[i];
                }
            }

            edgesSP.arraySize = edges.Count;
            if (edges.Count > 0)
            {
                var esp = edgesSP.GetArrayElementAtIndex(0);
                for (int i = 0; i < edges.Count; ++i, esp.Next(false))
                {
                    esp.vector2IntValue = edges[i];
                }
            }
        }
    }

    internal class SpritePhysicsOutlineDataTransfer : SpriteDataProviderBase, ISpritePhysicsOutlineDataProvider
    {
        public SpritePhysicsOutlineDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(dataProvider.GetSerializedObject(), dataProvider.spriteImportMode, index);
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spritePhysicsOutline = data;
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail = value;
        }

        private static List<Vector2[]> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var outlineSP = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_PhysicsShape") :
                importer.FindProperty("m_SpriteSheet.m_PhysicsShape");

            var outline = new List<Vector2[]>();
            if (outlineSP.arraySize > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < outlineSP.arraySize; ++j, outlinePathSP.Next(false))
                {
                    var o = new Vector2[outlinePathSP.arraySize];
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < outlinePathSP.arraySize; ++k, psp.Next(false))
                        {
                            o[k] = psp.vector2Value;
                        }
                    }
                    outline.Add(o);
                }
            }
            return outline;
        }

        public static void Apply(SerializedProperty rectSP, List<Vector2[]> value)
        {
            var outlineSP = rectSP.FindPropertyRelative("m_PhysicsShape");
            outlineSP.arraySize = value.Count;
            if (value.Count > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < value.Count; ++j, outlinePathSP.Next(false))
                {
                    var o = value[j];
                    outlinePathSP.arraySize = o.Length;
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < o.Length; ++k, psp.Next(false))
                        {
                            psp.vector2Value = o[k];
                        }
                    }
                }
            }
        }
    }

    internal class SpriteSecondaryTextureDataTransfer : SpriteDataProviderBase, ISecondaryTextureDataProvider
    {
        public SpriteSecondaryTextureDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        public SecondarySpriteTexture[] textures
        {
            get { return dataProvider.secondaryTextures; }
            set { dataProvider.secondaryTextures = value; }
        }

        public static SecondarySpriteTexture[] Load(SerializedObject so)
        {
            var secondaryTextures = so.FindProperty("m_SpriteSheet.m_SecondaryTextures");
            var returnValue = new SecondarySpriteTexture[secondaryTextures.arraySize];
            if (secondaryTextures.arraySize > 0)
            {
                var sp = secondaryTextures.GetArrayElementAtIndex(0);
                for (int i = 0; i < returnValue.Length; ++i, sp.Next(false))
                {
                    returnValue[i].name = sp.FindPropertyRelative("name").stringValue;
                    returnValue[i].texture = sp.FindPropertyRelative("texture").objectReferenceValue as Texture2D;
                }
            }
            return returnValue;
        }

        public static void Apply(SerializedObject so, SecondarySpriteTexture[] values)
        {
            var secondaryTextures = so.FindProperty("m_SpriteSheet.m_SecondaryTextures");
            secondaryTextures.arraySize = values.Length;
            if (values.Length > 0)
            {
                var e = secondaryTextures.GetArrayElementAtIndex(0);
                for (int i = 0; i < values.Length; ++i, e.Next(false))
                {
                    e.FindPropertyRelative("name").stringValue = values[i].name;
                    e.FindPropertyRelative("texture").objectReferenceValue = values[i].texture;
                }
            }
        }
    }

    internal class SpriteTextureDataTransfer : SpriteDataProviderBase, ITextureDataProvider
    {
        public SpriteTextureDataTransfer(TextureImporterDataProvider dp) : base(dp)
        {}

        Texture2D m_ReadableTexture;
        Texture2D m_OriginalTexture;

        public Texture2D texture
        {
            get
            {
                if (m_OriginalTexture == null)
                    m_OriginalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dataProvider.assetPath);
                return m_OriginalTexture;
            }
        }

        public Texture2D previewTexture
        {
            get { return texture; }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = height = 0;
            dataProvider.GetWidthAndHeight(ref width, ref height);
        }

        public Texture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                m_ReadableTexture = UnityEditor.SpriteUtility.CreateTemporaryDuplicate(texture, texture.width, texture.height);
                if (m_ReadableTexture != null)
                    m_ReadableTexture.filterMode = texture.filterMode;
            }
            return m_ReadableTexture;
        }
    }
}
