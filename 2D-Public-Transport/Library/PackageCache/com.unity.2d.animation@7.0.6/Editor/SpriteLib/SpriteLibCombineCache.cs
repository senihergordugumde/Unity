using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace UnityEditor.U2D.Animation
{
    internal class SpriteLibCombineCache : ScriptableObject
    {
        [SerializeField]
        List<SpriteLibCategoryOverride> m_Library = new List<SpriteLibCategoryOverride>();

        public List<SpriteLibCategoryOverride> library => m_Library;
    }
}
