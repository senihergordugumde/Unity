using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.U2D.Animation
{
    /// <summary>
    /// Component that holds a Sprite Library Asset. The component is used by SpriteResolver Component to query for Sprite based on Category and Index
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("2D Animation/Sprite Library")]
    [MovedFrom("UnityEngine.Experimental.U2D.Animation")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.2d.animation@latest/index.html?subfolder=/manual/SLAsset.html%23sprite-library-component")]
    public class SpriteLibrary : MonoBehaviour
    {
        struct CategoryEntrySprite
        {
            public string category;
            public string entry;
            public Sprite sprite;
        }

        [SerializeField]
        List<SpriteLibCategory> m_Library = new List<SpriteLibCategory>();

        [SerializeField]
        SpriteLibraryAsset m_SpriteLibraryAsset;
        
        // Cache for combining data in sprite library asset and main library
        Dictionary<int, CategoryEntrySprite> m_CategoryEntryHashCache = null;
        Dictionary<string, HashSet<string>> m_CategoryEntryCache = null;
        int m_PreviousSpriteLibraryAsset;
        long m_PreviousModificationHash;
        
        /// <summary>Get or Set the current SpriteLibraryAsset to use </summary>
        public SpriteLibraryAsset spriteLibraryAsset
        {
            set
            {
                if (m_SpriteLibraryAsset != value)
                {
                    m_SpriteLibraryAsset = value;
                    CacheOverrides();
                    RefreshSpriteResolvers();
                }
            }
            get { return m_SpriteLibraryAsset; }
        }

        void OnEnable()
        {
            CacheOverrides();
        }

        /// <summary>
        /// Return the Sprite that is registered for the given Category and Label for the SpriteLibrary
        /// </summary>
        /// <param name="category">Category name</param>
        /// <param name="label">Label name</param>
        /// <returns>Sprite associated to the name and index</returns>
        public Sprite GetSprite(string category, string label) 
        {
            return GetSprite(GetHashForCategoryAndEntry(category, label));
        }

        Sprite GetSprite(int hash)
        {
            if (m_CategoryEntryHashCache.ContainsKey(hash))
                return m_CategoryEntryHashCache[hash].sprite;
            return null;
        }

        void UpdateCacheOverridesIfNeeded()
        {
            if(m_CategoryEntryCache == null || 
                m_PreviousSpriteLibraryAsset != m_SpriteLibraryAsset?.GetInstanceID() ||
                m_PreviousModificationHash != m_SpriteLibraryAsset?.modificationHash)
                CacheOverrides();
        }

        internal bool GetCategoryAndEntryNameFromHash(int hash, out string category, out string entry)
        {
            UpdateCacheOverridesIfNeeded();
            if (m_CategoryEntryHashCache.ContainsKey(hash))
            {
                category = m_CategoryEntryHashCache[hash].category;
                entry = m_CategoryEntryHashCache[hash].entry;
                return true;
            }

            category = null;
            entry = null;
            return false;
        }

        internal static int GetHashForCategoryAndEntry(string category, string entry)
        {
            return SpriteLibraryAsset.GetStringHash($"{category}_{entry}");
        }

        internal Sprite GetSpriteFromCategoryAndEntryHash(int hash, out bool validEntry)
        {
            UpdateCacheOverridesIfNeeded();
            if (m_CategoryEntryHashCache.ContainsKey(hash))
            {
                validEntry = true;
                return m_CategoryEntryHashCache[hash].sprite;
            }
            validEntry = false;
            return null;
        }

        List<SpriteCategoryEntry> GetEntries(string category, bool addIfNotExist)
        {
            var index = m_Library.FindIndex(x => x.name == category);
            if (index < 0)
            {
                if (!addIfNotExist)
                    return null;
                m_Library.Add(new SpriteLibCategory()
                {
                    name = category,
                    categoryList = new List<SpriteCategoryEntry>()
                });
                index = m_Library.Count - 1;
            }

            return m_Library[index].categoryList;
        }

        SpriteCategoryEntry GetEntry(List<SpriteCategoryEntry> entries, string entry, bool addIfNotExist)
        {
            var index = entries.FindIndex(x => x.name == entry);
            if (index < 0)
            {
                if (!addIfNotExist)
                    return null;
                entries.Add(new SpriteCategoryEntry()
                {
                    name = entry,
                });
                index = entries.Count - 1;
            }

            return entries[index];
        }
        
        /// <summary>
        /// Add or replace an override when querying for the given Category and Label from a SpriteLibraryAsset
        /// </summary>
        /// <param name="spriteLib">Sprite Library Asset to query</param>
        /// <param name="category">Category name from the Sprite Library Asset to add override</param>
        /// <param name="label">Label name to add override</param>
        public void AddOverride(SpriteLibraryAsset spriteLib, string category, string label)
        {
            var sprite = spriteLib.GetSprite(category, label);
            var entries = GetEntries(category, true);
            var entry = GetEntry(entries, label, true);
            entry.sprite = sprite;
            CacheOverrides();
        }

        /// <summary>
        /// Add or replace an override when querying for the given Category. All the categories in the Category will be added.
        /// </summary>
        /// <param name="spriteLib">Sprite Library Asset to query</param>
        /// <param name="category">Category name from the Sprite Library Asset to add override</param>
        public void AddOverride(SpriteLibraryAsset spriteLib, string category)
        {
            var categoryHash = SpriteLibraryAsset.GetStringHash(category);
            var cat = spriteLib.categories.FirstOrDefault(x => x.hash == categoryHash);
            if (cat != null)
            {
                var entries = GetEntries(category, true);
                for (var i = 0; i < cat.categoryList.Count; ++i)
                {
                    var ent = cat.categoryList[i];
                    GetEntry(entries, ent.name, true).sprite = ent.sprite;
                }
                CacheOverrides();
            }
        }

        /// <summary>
        /// Add or replace an override when querying for the given Category and Label.
        /// </summary>
        /// <param name="sprite">Sprite to override to</param>
        /// <param name="category">Category name to override</param>
        /// <param name="label">Label name to override</param>
        public void AddOverride(Sprite sprite, string category, string label)
        {
            GetEntry(GetEntries(category, true), label, true).sprite = sprite;
            CacheOverrides();
            RefreshSpriteResolvers();
        }

        /// <summary>
        /// Remove all Sprite Library override for a given category
        /// </summary>
        /// <param name="category">Category overrides to remove</param>
        public void RemoveOverride(string category)
        {
            var index = m_Library.FindIndex(x => x.name == category);
            if (index >= 0)
            {
                m_Library.RemoveAt(index);
                CacheOverrides();
                RefreshSpriteResolvers();
            }
        }

        /// <summary>
        /// Remove Sprite Library override for a given category and label
        /// </summary>
        /// <param name="category">Category to remove</param>
        /// <param name="label">Label to remove</param>
        public void RemoveOverride(string category, string label)
        {
            var entries = GetEntries(category, false);
            if (entries != null)
            {
                var index = entries.FindIndex(x => x.name == label);
                if (index >= 0)
                {
                    entries.RemoveAt(index);
                    CacheOverrides();
                    RefreshSpriteResolvers();
                }
            }
        }

        /// <summary>
        /// Method to check if a Category and Label pair has an override
        /// </summary>
        /// <param name="category">Category name</param>
        /// <param name="label">Label name</param>
        /// <returns>True if override exist, false otherwise</returns>
        public bool HasOverride(string category, string label)
        {
            var catOverride = GetEntries(category, false);
            if (catOverride != null)
                return GetEntry(catOverride, label, false) != null;
            return false;
        }

        /// <summary>
        /// Request SpriteResolver components that are in the same hierarchy to refresh
        /// </summary>
        public void RefreshSpriteResolvers()
        {
            var spriteResolvers = GetComponentsInChildren<SpriteResolver>();
            foreach (var sr in spriteResolvers)
            {
                sr.ResolveSpriteToSpriteRenderer();
#if UNITY_EDITOR
                sr.spriteLibChanged = true;
#endif
            }
        }

        internal IEnumerable<string> categoryNames
        {
            get
            {
                UpdateCacheOverridesIfNeeded();
                return m_CategoryEntryCache.Keys;
            }
        }

        internal IEnumerable<string> GetEntryNames(string category)
        {
            UpdateCacheOverridesIfNeeded();
            if (m_CategoryEntryCache.ContainsKey(category))
                return m_CategoryEntryCache[category];
            return null;
        }

        internal void CacheOverrides()
        {
            m_PreviousSpriteLibraryAsset = 0;
            m_PreviousModificationHash = 0;
            m_CategoryEntryHashCache = new Dictionary<int, CategoryEntrySprite>();
            m_CategoryEntryCache = new Dictionary<string, HashSet<string>>();
            if (m_SpriteLibraryAsset)
            {
                m_PreviousSpriteLibraryAsset = m_SpriteLibraryAsset.GetInstanceID();
                m_PreviousModificationHash = m_SpriteLibraryAsset.modificationHash;
                foreach (var category in m_SpriteLibraryAsset.categories)
                {
                    var catName = category.name;
                    m_CategoryEntryCache.Add(catName, new HashSet<string>());
                    var cacheEntryName = m_CategoryEntryCache[catName];
                    foreach (var entry in category.categoryList)
                    {
                        m_CategoryEntryHashCache.Add(GetHashForCategoryAndEntry(catName, entry.name), new CategoryEntrySprite()
                        {
                            category = catName,
                            entry = entry.name,
                            sprite = entry.sprite
                        });
                        cacheEntryName.Add(entry.name);
                    }
                }
            }
            
            foreach (var category in m_Library)
            {
                var catName = category.name;
                if(!m_CategoryEntryCache.ContainsKey(catName))
                    m_CategoryEntryCache.Add(catName, new HashSet<string>());
                var cacheEntryName = m_CategoryEntryCache[catName];
                
                foreach (var ent in category.categoryList)
                {
                    if (!cacheEntryName.Contains(ent.name))
                        cacheEntryName.Add(ent.name);

                    var hash = GetHashForCategoryAndEntry(catName, ent.name);
                    if (!m_CategoryEntryHashCache.ContainsKey(hash))
                    {
                        m_CategoryEntryHashCache.Add(hash, new CategoryEntrySprite()
                        {
                            category = catName,
                            entry = ent.name,
                            sprite = ent.sprite
                        });
                    }
                    else
                    {
                        var e = m_CategoryEntryHashCache[hash];
                        e.sprite = ent.sprite;
                        m_CategoryEntryHashCache[hash] = e;
                    }
                }
            }
        }
    }
}
