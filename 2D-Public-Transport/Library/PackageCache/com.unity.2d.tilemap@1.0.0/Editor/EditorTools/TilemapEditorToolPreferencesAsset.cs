using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    [Serializable]
    internal struct DefaultTilemapEditorTool
    {
        public string fullTypeName;
        [NonSerialized]
        public Type tilemapEditorToolType;
        [NonSerialized]
        public TilemapEditorTool toolInstance;
    }

    internal class TilemapEditorToolPreferencesAsset : ScriptableObject
    {
        public List<DefaultTilemapEditorTool> m_UserDefaultTools;
    }
}
