using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.U2D.Animation
{
    [Serializable]
    internal class SpriteCategoryEntryOverride : SpriteCategoryEntry
    {
        [SerializeField]
        bool m_FromMain;
        [SerializeField]
        Sprite m_SpriteOverride;
        public bool fromMain
        {
            get => m_FromMain;
            set => m_FromMain = value;
        }

        public Sprite spriteOverride
        {
            get => m_SpriteOverride;
            set => m_SpriteOverride = value;
        }
    }

    [Serializable]
    internal class SpriteLibCategoryOverride : SpriteLibCategory
    {
        [SerializeField]
        private List<SpriteCategoryEntryOverride> m_OverrideEntries;
        [SerializeField]
        bool m_FromMain;
        [SerializeField]
        int m_EntryOverrideCount;
        
        public bool fromMain
        {
            get => m_FromMain;
            set => m_FromMain = value;
        }
            
        public int entryOverrideCount
        {
            get => m_EntryOverrideCount;
            set => m_EntryOverrideCount = value;
        }
        
        public List<SpriteCategoryEntryOverride> overrideEntries
        {
            get { return m_OverrideEntries; }
            set { m_OverrideEntries = value; }
        }
    }
}