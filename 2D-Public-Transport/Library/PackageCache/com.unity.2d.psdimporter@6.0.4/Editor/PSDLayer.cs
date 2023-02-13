using System;
using Unity.Collections;
using UnityEngine;

namespace UnityEditor.U2D.PSD
{
    [Serializable]
    class PSDLayer : IPSDLayerMappingStrategyComparable
    {
        [SerializeField]
        string m_Name;
        [SerializeField]
        string m_SpriteName;
        [SerializeField]
        bool m_IsGroup;
        [SerializeField]
        int m_ParentIndex;
        [SerializeField]
        string m_SpriteID;
        [SerializeField]
        int m_LayerID;
        [SerializeField]
        Vector2Int m_MosaicPosition;
        [SerializeField]
        bool m_Flatten;
        [SerializeField]
        bool m_IsImported;
        [SerializeField]
        bool m_IsVisible;

        [NonSerialized]
        GameObject m_GameObject;

        public PSDLayer(NativeArray<Color32> tex, int parent, bool group, string layerName, int width, int height, int id, bool hidden)
        {
            isGroup = group;
            parentIndex = parent;
            texture = tex;
            name = layerName;
            this.width = width;
            this.height = height;
            layerID = id;
            m_Flatten = false;
            m_IsImported = false;
            m_IsVisible = hidden;
            m_SpriteID = new GUID().ToString();
        }

        public bool isVisible => m_IsVisible;
        public int layerID { get { return m_LayerID; } private set { m_LayerID = value; } }

        public string name { get { return m_Name; } private set { m_Name = value; } }
        public string spriteName { get { return m_SpriteName; } set { m_SpriteName = value; } }
        public bool isGroup { get { return m_IsGroup; } private set { m_IsGroup = value; } }
        public int parentIndex { get { return m_ParentIndex; } private set { m_ParentIndex = value; } }
        public Vector2Int mosaicPosition { get { return m_MosaicPosition; } set { m_MosaicPosition = value; } }
        public GUID spriteID  { get { return new GUID(m_SpriteID); } set { m_SpriteID = value.ToString(); } }
        public GameObject gameObject { get { return m_GameObject; } set { m_GameObject = value; } }

        public bool flatten
        {
            get => m_Flatten;
            set => m_Flatten = value;
        }

        public bool isImported
        {
            get => m_IsImported;
            set => m_IsImported = value;
        }

        public NativeArray<Color32> texture { get; set; }
        public int width { get; private set; }
        public int height { get; private set; }

        public void Dispose()
        {
            if (texture.IsCreated)
                texture.Dispose();
        }
    }
}
