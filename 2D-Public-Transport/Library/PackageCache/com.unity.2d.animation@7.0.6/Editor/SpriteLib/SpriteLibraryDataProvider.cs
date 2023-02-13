using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.U2D.Animation
{
    /// <summary>
    /// Structure that defines a Sprite Library Category Label
    /// </summary>
    [Serializable]
    public struct SpriteCategoryLabel
    {
        [SerializeField]
        string m_Name;
        [SerializeField]
        string m_SpriteId;

        /// <summary>
        /// Get and set the name for the Sprite label
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Get and set the Sprite Id.
        /// </summary>
        public string spriteId
        {
            get { return m_SpriteId; }
            set { m_SpriteId = value; }
        }
    }

    /// <summary>
    /// Structure that defines a Sprite Library Category.
    /// </summary>
    [Serializable]
    public struct SpriteCategory
    {
        [SerializeField]
        [FormerlySerializedAs("name")]
        string m_Name;
        [SerializeField]
        List<SpriteCategoryLabel> m_Labels;

        /// <summary>
        /// Get and set the name for the Sprite Category
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Get and set the Sprites registered to this category.
        /// </summary>
        public List<SpriteCategoryLabel> labels
        {
            get { return m_Labels; }
            set { m_Labels = value; }
        }
    }

    /// <summary>
    /// A structure to hold a collection of SpriteCategory
    /// </summary>
    [Serializable]
    public struct SpriteCategoryList
    {
        [SerializeField]
        [FormerlySerializedAs("categories")]
        List<SpriteCategory> m_Categories;

        /// <summary>
        /// Get or set the a list of SpriteCategory
        /// </summary>
        public List<SpriteCategory> categories
        {
            get { return m_Categories; }
            set { m_Categories = value; }
        }
    }

    /// <summary>An interface that allows Sprite Editor Modules to edit Sprite Library data for user custom importer.</summary>
    /// <remarks>Implement this interface for [[ScriptedImporter]] to leverage on Sprite Editor Modules to edit Sprite Library data.</remarks>
    [Obsolete("The interface is no longer used")]
    public interface ISpriteLibDataProvider
    {
        /// <summary>
        /// Returns the SpriteCategoryList structure that represents the Sprite Library data.
        /// </summary>
        /// <returns>SpriteCategoryList data</returns>
        SpriteCategoryList GetSpriteCategoryList();


        /// <summary>
        /// Sets the SpriteCategoryList structure that represents the Sprite Library data to the data provider
        /// </summary>
        /// <param name="spriteCategoryList">Data to set</param>
        void SetSpriteCategoryList(SpriteCategoryList spriteCategoryList);
    }
}
