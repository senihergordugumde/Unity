using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D.Animation;

namespace UnityEditor.U2D.Animation
{
    [CustomEditor(typeof(SpriteLibrary))]
    [CanEditMultipleObjects]
    [MovedFrom("UnityEditor.Experimental.U2D.Animation")]
    internal class SpriteLibraryInspector : Editor
    {
        static class Style
        {
            public static string libraryDifferentValue = L10n.Tr("Sprite Library has different values.");
        }
        
        SpriteLibCombineCache m_OverrideLibraryObject;
        SerializedObject m_OverrideLibraryCache;
        SerializedProperty m_MasterLibraryProperty;
        SpriteLibraryAsset m_MasterLibraryObject;
        SerializedProperty m_MasterLibraryCategories;
        SerializedProperty m_OverrideLibraryCategories;
        SpriteLibraryDataInspector m_SpriteLibraryDataInspector;
        long m_PreviousModificationHash;

        List<SpriteLibrary> m_CachedLibraryTargets = new List<SpriteLibrary>();
        List<SpriteResolver> m_CachedResolvers = new List<SpriteResolver>();

        public void OnEnable()
        {
            m_OverrideLibraryObject = ScriptableObject.CreateInstance<SpriteLibCombineCache>();
            m_OverrideLibraryCache = new SerializedObject(m_OverrideLibraryObject);
            m_OverrideLibraryCategories = m_OverrideLibraryCache.FindProperty("m_Library");
            
            m_MasterLibraryProperty = serializedObject.FindProperty("m_SpriteLibraryAsset");
            m_MasterLibraryCategories = serializedObject.FindProperty("m_Library");

            UpdateMasterLibraryObject();
            CacheTargets();
            UpdateSpriteLibraryDataCache();
        }

        void UpdateMasterLibraryObject()
        {
            m_MasterLibraryObject = (SpriteLibraryAsset)m_MasterLibraryProperty.objectReferenceValue;
        }

        void CacheTargets()
        {
            m_CachedLibraryTargets.Clear();
            foreach(var t in targets)
                m_CachedLibraryTargets.Add(t as SpriteLibrary);

            m_CachedResolvers.Clear();
            foreach (var sl in m_CachedLibraryTargets)
            {
                var resolvers = sl.GetComponentsInChildren<SpriteResolver>();
                m_CachedResolvers.AddRange(resolvers);
            }
        }

        void UpdateSpriteLibraryDataCache()
        {
            if(m_MasterLibraryCategories.hasMultipleDifferentValues)
                return;
            if (m_MasterLibraryProperty.hasMultipleDifferentValues)
                return;
            
            CopySpriteLibraryToOverride(m_OverrideLibraryObject.library, m_MasterLibraryCategories);
            m_OverrideLibraryCache.Update();
            SpriteLibraryDataInspector.UpdateLibraryWithNewMainLibrary(m_MasterLibraryObject, m_OverrideLibraryCategories);
            m_SpriteLibraryDataInspector = new SpriteLibraryDataInspector(serializedObject, m_OverrideLibraryCategories);
            m_OverrideLibraryCache.ApplyModifiedPropertiesWithoutUndo();
        }
        
        public override void OnInspectorGUI()
        {
            RefreshMasterLibraryAssetData();
            
            serializedObject.Update();
            m_OverrideLibraryCache.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_MasterLibraryProperty);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateMasterLibraryObject();
                UpdateSpriteLibraryDataCache();
                serializedObject.ApplyModifiedProperties();
                UpdateSpriteResolvers();
            }

            if (m_SpriteLibraryDataInspector != null)
            {
                EditorGUI.BeginChangeCheck();
                m_SpriteLibraryDataInspector.OnGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    m_OverrideLibraryCache.ApplyModifiedProperties();
                    CopyOverrideToSpriteLibrary(m_OverrideLibraryObject.library, m_MasterLibraryCategories);
                    serializedObject.ApplyModifiedProperties();

                    foreach(var spriteLib in m_CachedLibraryTargets)
                        spriteLib.CacheOverrides();

                    UpdateSpriteResolvers();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(Style.libraryDifferentValue, MessageType.Info);
            }
        }

        void RefreshMasterLibraryAssetData()
        {
            var modificationHash = m_MasterLibraryObject ? m_MasterLibraryObject.modificationHash : 0;
            if (m_PreviousModificationHash != modificationHash)
            {
                UpdateSpriteLibraryDataCache();
                UpdateSpriteResolvers();
                m_PreviousModificationHash = modificationHash;
            }            
        }

        void UpdateSpriteResolvers()
        {
            foreach (var resolver in m_CachedResolvers)
            {
                resolver.ResolveSpriteToSpriteRenderer();
                resolver.spriteLibChanged = true;
            } 
        }

        static void CopySpriteLibraryToOverride(List<SpriteLibCategoryOverride> libOverride, SerializedProperty lib)
        {
            libOverride.Clear();
            if (lib.arraySize == 0)
                return;
            
            var categoryEntries = lib.GetArrayElementAtIndex(0);
            for (var i = 0; i < lib.arraySize; ++i)
            {
                var overrideCategory = new SpriteLibCategoryOverride()
                {
                    categoryList = new List<SpriteCategoryEntry>(),
                    entryOverrideCount = 0,
                    fromMain = false,
                    name = categoryEntries.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue,
                    overrideEntries = new List<SpriteCategoryEntryOverride>()
                };
                var entries = categoryEntries.FindPropertyRelative(SpriteLibraryPropertyString.categoryList);
                var overrideCategoryEntries = overrideCategory.overrideEntries;
                if (entries.arraySize > 0)
                {
                    var entry = entries.GetArrayElementAtIndex(0); 
                    for (var j = 0; j < entries.arraySize; ++j)
                    {
                        overrideCategoryEntries.Add(new SpriteCategoryEntryOverride()
                        {
                            fromMain = false,
                            name = entry.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue,
                            sprite = (Sprite)entry.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue,
                            spriteOverride = (Sprite)entry.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue
                        });
                        entry.Next(false);
                    }
                }
                libOverride.Add(overrideCategory);
                categoryEntries.Next(false);
            }
        }
        
        static void CopyOverrideToSpriteLibrary(List<SpriteLibCategoryOverride> libOverride, SerializedProperty lib)
        {
            lib.arraySize = 0;
            if (libOverride.Count == 0)
                return;
            
            for (var i = 0; i < libOverride.Count; ++i)
            {
                var libOverrideElement = libOverride[i];
                if (!libOverrideElement.fromMain || libOverrideElement.entryOverrideCount > 0)
                {
                    lib.arraySize += 1;
                    var libElement = lib.GetArrayElementAtIndex(lib.arraySize - 1);
                    libElement.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue = libOverrideElement.name;
                    var overrideEntries = libOverrideElement.overrideEntries;
                    var entries = libElement.FindPropertyRelative(SpriteLibraryPropertyString.categoryList);
                    entries.arraySize = 0;
                    if (overrideEntries.Count > 0)
                    {
                        for (var j = 0; j < overrideEntries.Count; ++j)
                        {
                            var overrideEntry = overrideEntries[j];
                            if (!overrideEntry.fromMain ||
                                overrideEntry.sprite != overrideEntry.spriteOverride)
                            {
                                entries.arraySize += 1;
                                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                                entry.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue = overrideEntry.name;
                                entry.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue = overrideEntry.spriteOverride;
                            }
                        }
                    }
                }
            }
        }
    }
}
