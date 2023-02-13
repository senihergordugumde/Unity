using System.Collections.Generic;

namespace UnityEngine.U2D.Animation
{
    internal class SpriteLibrarySourceAsset : ScriptableObject
    {
        public const string defaultName = "New Sprite Library Asset";
        public const string extension = ".spriteLib";
        
        [SerializeField]
        List<SpriteLibCategoryOverride> m_Library = new List<SpriteLibCategoryOverride>();
        [SerializeField]
        string m_PrimaryLibraryGUID;

        public IReadOnlyList<SpriteLibCategoryOverride> library => m_Library;
        
        public void InitializeWithAsset(SpriteLibrarySourceAsset source)
        {
            m_Library = new List<SpriteLibCategoryOverride>(source.m_Library);
            m_PrimaryLibraryGUID = source.m_PrimaryLibraryGUID;
        }
        
        public void SetLibrary(IList<SpriteLibCategoryOverride> newLibrary)
        {
            if (!m_Library.Equals(newLibrary))
            {
                m_Library = new List<SpriteLibCategoryOverride>(newLibrary);
            }
        }
        
        public void SetPrimaryLibraryGUID(string newPrimaryLibraryGUID)
        {
            m_PrimaryLibraryGUID = newPrimaryLibraryGUID;
        }        
        
        public void AddCategory(SpriteLibCategoryOverride newCategory)
        {
            if (!m_Library.Contains(newCategory))
            {
                m_Library.Add(newCategory);
            }
        }

        public void RemoveCategory(SpriteLibCategoryOverride categoryToRemove)
        {
            if (m_Library.Contains(categoryToRemove))
            {
                m_Library.Remove(categoryToRemove);
            }
        }

        public void RemoveCategory(int indexToRemove)
        {
            if (indexToRemove >= 0 && indexToRemove < m_Library.Count)
            {
                m_Library.RemoveAt(indexToRemove);
            }
        }        

        public string primaryLibraryID
        {
            get => m_PrimaryLibraryGUID;
            set => m_PrimaryLibraryGUID = value;
        }
    }
}