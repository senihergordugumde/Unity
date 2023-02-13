using System;
using System.Collections.Generic;
using PDNWrapper;
using UnityEngine;

namespace UnityEditor.U2D.PSD
{
    internal class PSDImportData : ScriptableObject
    {
        [SerializeField]
        int m_ImportedTextureWidth;
        public int importedTextureWidth
        {
            get => m_ImportedTextureWidth;
            set => m_ImportedTextureWidth = value;
        }
        
        [SerializeField]
        public int m_ImportedTextureHeight;
        public int importedTextureHeight
        {
            get => m_ImportedTextureHeight;
            set => m_ImportedTextureHeight = value;
        }
        
        [SerializeField]
        Vector2Int m_DocumentSize;
        public Vector2Int documentSize
        {
            get => m_DocumentSize;
            set => m_DocumentSize = value;
        }

        [SerializeField]
        int m_TextureActualHeight;
        public int textureActualHeight
        {
            get => m_TextureActualHeight;
            set => m_TextureActualHeight = value;
        }

        [SerializeField]
        int m_TextureActualWidth;
        public int textureActualWidth
        {
            get => m_TextureActualWidth;
            set => m_TextureActualWidth = value;
        }

        [SerializeField]
        PSDLayerData[] m_PsdLayerData;
        public PSDLayerData[] psdLayerData
        {
            get => m_PsdLayerData;
        }

        public void CreatePSDLayerData(List<BitmapLayer> bitmapLayer)
        {
            var layerData = new List<PSDLayerData>();
            foreach (var fileLayer in bitmapLayer)
            {
                CreatePSDLayerData(fileLayer, layerData);
            }
            m_PsdLayerData = layerData.ToArray();
        }

        void CreatePSDLayerData(BitmapLayer layer, List<PSDLayerData> layerData, int parentIndex = -1)
        {
            layerData.Add(new PSDLayerData()
            {
                isGroup = layer.IsGroup,
                isVisible = layer.Visible,
                layerID = layer.LayerID,
                name = layer.Name,
                parentIndex = parentIndex
            });
            parentIndex = layerData.Count - 1;
            foreach (var fileLayer in layer.ChildLayer)
            {
                CreatePSDLayerData(fileLayer, layerData, parentIndex);
            }
        }
    }

    // Struct to keep track of GOs and bone
    internal struct BoneGO
    {
        public GameObject go;
        public int index;
    }
    
    internal class FlattenLayerData: IPSDLayerMappingStrategyComparable
    {
        public int layerID { get; set; }
        public string name { get; set;}
        public bool isGroup => true;
    }

    [Serializable]
    class PSDLayerImportSetting
    {
        public string name;
        public int layerId;
        public bool flatten;
        public bool isGroup;
    }
    
    [Serializable]
    class PSDLayerData : IPSDLayerMappingStrategyComparable
    {
        [SerializeField]
        string m_Name;
        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        [SerializeField]
        int m_ParentIndex;
        public int parentIndex
        {
            get => m_ParentIndex;
            set => m_ParentIndex = value;
        }

        [SerializeField]
        int m_LayerID;
        public int layerID
        {
            get => m_LayerID;
            set => m_LayerID = value;
        }

        [SerializeField]
        bool m_IsVisible;
        public bool isVisible
        {
            get => m_IsVisible;
            set => m_IsVisible = value;
        }

        [SerializeField]
        bool m_IsGroup;
        public bool isGroup
        {
            get => m_IsGroup;
            set => m_IsGroup = value;
        }
    }
    
    [Serializable]
    struct SpriteLayerMapping
    {
        public string spriteId;
        public int layerId;
        public string layerName;
    }
}

