using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.U2D.Animation
{
    /// <summary>
    /// Asset representing character's Skeleton.
    /// </summary>
    public class SkeletonAsset : ScriptableObject
    {
        [SerializeField] private SpriteBone[] m_SpriteBones;

        /// <summary>
        /// Allows to get Skeleton bones.
        /// </summary>
        /// <returns>Skeleton's SpriteBone array.</returns>
        public SpriteBone[] GetSpriteBones()
        {
            return m_SpriteBones;
        }

        /// <summary>
        /// Allows to set new Skeleton bones.
        /// </summary>
        /// <param name="spriteBones">New SpriteBone array.</param>
        public void SetSpriteBones(SpriteBone[] spriteBones)
        {
            m_SpriteBones = spriteBones;
        }
    }    
}

