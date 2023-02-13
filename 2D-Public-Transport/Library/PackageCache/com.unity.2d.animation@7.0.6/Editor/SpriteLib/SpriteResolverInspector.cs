using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D.Animation;

namespace UnityEditor.U2D.Animation
{
    [CustomEditor(typeof(SpriteResolver))]
    [MovedFrom("UnityEditor.Experimental.U2D.Animation")]
    internal class SpriteResolverInspector : Editor
    {
        static class Style
        {
            public static GUIContent categoryLabel = EditorGUIUtility.TrTextContent("Category");
            public static GUIContent labelLabel = EditorGUIUtility.TrTextContent("Label");
            public static GUIContent categoryIsEmptyLabel = EditorGUIUtility.TrTextContent("Category is Empty");
            public static GUIContent noCategory = EditorGUIUtility.TrTextContent("No Category");
            public static string[] emptyCategoryDropDownOption = new[] { Style.categoryIsEmptyLabel.text };
        }

        struct SpriteCategorySelectionList
        {
            public string categoryName;
            public string[] entryNames;
            public Sprite[] sprites;
        }

        SerializedProperty m_SpriteKey;
        SerializedProperty m_LabelHash;
        SerializedProperty m_CategoryHash;
        SpriteSkin m_SpriteSkin;
        Dictionary<string, SpriteCategorySelectionList> m_SpriteLibSelection = new Dictionary<string, SpriteCategorySelectionList>();
        string[] m_CategorySelection;
        int m_CategorySelectionIndex = 0;
        int m_LabelSelectionIndex = 0;
        string m_PreviousCategoryValue;
        string m_PreviousLabelValue;
        bool m_IgnoreNextDeserializeCallback;
        bool m_ReInitOnNextGUI;
        SpriteSelectorWidget m_SpriteSelectorWidget = new SpriteSelectorWidget();

        public void OnEnable()
        {
            m_SpriteKey = serializedObject.FindProperty("m_SpriteKey");
            m_LabelHash = serializedObject.FindProperty("m_labelHash");
            m_CategoryHash = serializedObject.FindProperty("m_CategoryHash");
            m_SpriteSkin = (target as SpriteResolver).GetComponent<SpriteSkin>();
            UpdateSpriteLibrary();
            spriteResolver.onDeserializedCallback += SpriteResolverDeserializedCallback;
        }

        void SpriteResolverDeserializedCallback()
        {
            if (!m_IgnoreNextDeserializeCallback)
            {
                m_ReInitOnNextGUI = true;
            }
        }
        
        SpriteResolver spriteResolver => target as SpriteResolver;

        void GetCategoryAndLabelStringValue(out string categoryName, out string labelName)
        {
            categoryName = null;
            labelName = null;
            var spriteLib = spriteResolver.spriteLibrary;
            if (spriteLib != null)
            {
                int entryHash = SpriteResolver.ConvertFloatToInt(m_SpriteKey.floatValue);
                spriteLib.GetCategoryAndEntryNameFromHash(entryHash, out categoryName, out labelName);
                if (string.IsNullOrEmpty(categoryName) || string.IsNullOrEmpty(labelName))
                {
                    int labelHash = SpriteResolver.ConvertFloatToInt(m_LabelHash.floatValue);
                    int categoryHash = SpriteResolver.ConvertFloatToInt(m_CategoryHash.floatValue);
                    m_SpriteKey.floatValue = SpriteResolver.ConvertCategoryLabelHashToSpriteKey(spriteLib, categoryHash, labelHash);
                    entryHash = SpriteResolver.ConvertFloatToInt(m_SpriteKey.floatValue);
                    spriteLib.GetCategoryAndEntryNameFromHash(entryHash, out categoryName, out labelName);
                }
            }
        }
        
        void UpdateSpriteLibrary()
        {
            m_SpriteLibSelection.Clear();
            
            var spriteLib = spriteResolver.spriteLibrary;
            string categoryName ="", labelName ="";
            if (spriteLib != null)
            {
                GetCategoryAndLabelStringValue(out categoryName, out labelName);
                var enumerator = spriteLib.categoryNames;
                foreach(var category in spriteLib.categoryNames)
                {
                    if (!m_SpriteLibSelection.ContainsKey(category))
                    {
                        var entries = spriteLib.GetEntryNames(category);
                        if (entries == null)
                            entries = new string[0];
                        
                        var selectionList = new SpriteCategorySelectionList()
                        {
                            entryNames = entries.ToArray(),
                            sprites = entries.Select(x =>
                            {
                                return spriteLib.GetSprite(category, x);
                            }).ToArray(),
                            categoryName = category,
                        };

                        m_SpriteLibSelection.Add(category, selectionList);
                        
                    }
                }
            }
            m_CategorySelection = new string[1 + m_SpriteLibSelection.Keys.Count];
            m_CategorySelection[0] = Style.noCategory.text;
            for (int i = 0; i < m_SpriteLibSelection.Keys.Count; ++i)
            {
                var selection = m_SpriteLibSelection[m_SpriteLibSelection.Keys.ElementAt(i)];
                m_CategorySelection[i + 1] = selection.categoryName;
                if (selection.categoryName == categoryName)
                    m_CategorySelectionIndex = i + 1;
            }
            ValidateCategorySelectionIndexValue();
            if (m_CategorySelectionIndex > 0)
            {
                categoryName = m_CategorySelection[m_CategorySelectionIndex];
                m_SpriteSelectorWidget.UpdateContents(
                    m_SpriteLibSelection[m_CategorySelection[m_CategorySelectionIndex]].sprites);
                if (m_SpriteLibSelection.ContainsKey(categoryName))
                {
                    var labelIndex = Array.FindIndex(m_SpriteLibSelection[categoryName].entryNames,
                        x => x == labelName);
                    
                    if (labelIndex >= 0 ||
                        m_SpriteLibSelection[categoryName].entryNames.Length <= m_LabelSelectionIndex)
                    {
                        m_LabelSelectionIndex = labelIndex;
                    }
                }
            }
            else
            {
                m_SpriteSelectorWidget.UpdateContents(new Sprite[0]);
            }
            spriteResolver.spriteLibChanged = false;
        }

        void ValidateCategorySelectionIndexValue()
        {
            if (m_CategorySelectionIndex < 0 || m_CategorySelection.Length <= m_CategorySelectionIndex)
                m_CategorySelectionIndex = 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (m_ReInitOnNextGUI)
            {
                m_ReInitOnNextGUI = false;
                UpdateSpriteLibrary();
            }

            if (spriteResolver.spriteLibChanged)
                UpdateSpriteLibrary();

            GetCategoryAndLabelStringValue(out var currentCategoryValue, out var currentLabelValue);
            var catIndex = Array.FindIndex(m_CategorySelection, x => x == currentCategoryValue);
            if (catIndex >= 0)
                m_CategorySelectionIndex = catIndex;
            ValidateCategorySelectionIndexValue();
            
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(m_CategorySelection.Length <= 1))
                m_CategorySelectionIndex = EditorGUILayout.Popup(Style.categoryLabel, m_CategorySelectionIndex, m_CategorySelection);
            
            SpriteCategorySelectionList selection;
            m_SpriteLibSelection.TryGetValue(m_CategorySelection[m_CategorySelectionIndex], out selection);

            var entryNames = Style.emptyCategoryDropDownOption;
            if (selection.entryNames != null)
                entryNames = selection.entryNames;
            if (m_LabelSelectionIndex < 0 || m_LabelSelectionIndex >= entryNames.Length)
                m_LabelSelectionIndex = 0;
            using (new EditorGUI.DisabledScope(m_CategorySelectionIndex == 0 || entryNames.Length == 0))
            {
                if (entryNames.Length == 0)
                {
                    m_LabelSelectionIndex = EditorGUILayout.Popup(Style.labelLabel, 0, new [] {Style.categoryIsEmptyLabel});
                }
                else
                {
                    m_LabelSelectionIndex = EditorGUILayout.Popup(Style.labelLabel, m_LabelSelectionIndex, entryNames);
                }
            }
                
            m_LabelSelectionIndex = m_SpriteSelectorWidget.ShowGUI(m_LabelSelectionIndex);
            

            if (EditorGUI.EndChangeCheck())
            {
                currentCategoryValue = m_CategorySelection[m_CategorySelectionIndex];
                if (m_SpriteLibSelection.ContainsKey(currentCategoryValue))
                {
                    var hash = m_SpriteLibSelection[currentCategoryValue].entryNames;
                    if (hash.Length > 0)
                    {
                        if (m_LabelSelectionIndex < 0 || m_LabelSelectionIndex >= hash.Length)
                            m_LabelSelectionIndex = 0;
                        currentLabelValue = m_SpriteLibSelection[currentCategoryValue].entryNames[m_LabelSelectionIndex];
                    }
                }

                m_SpriteKey.floatValue = SpriteResolver.ConvertIntToFloat(SpriteLibrary.GetHashForCategoryAndEntry(currentCategoryValue, currentLabelValue));
                ApplyModifiedProperty();

                var sf = target as SpriteResolver;
                if (m_SpriteSkin != null)
                    m_SpriteSkin.ignoreNextSpriteChange = true;
                sf.ResolveSpriteToSpriteRenderer();
            }

            if (m_PreviousCategoryValue != currentCategoryValue)
            {
                if (!string.IsNullOrEmpty(currentCategoryValue))
                {
                    if (m_SpriteLibSelection.ContainsKey(currentCategoryValue))
                    {
                        m_SpriteSelectorWidget.UpdateContents(m_SpriteLibSelection[currentCategoryValue].sprites);
                    }
                    else
                        m_SpriteSelectorWidget.UpdateContents(new Sprite[0]);
                    
                    this.Repaint();
                }
                
                m_PreviousCategoryValue = currentCategoryValue;
            }

            if (!string.IsNullOrEmpty(currentLabelValue) && m_PreviousLabelValue != currentLabelValue)
            {
                if (m_SpriteLibSelection.ContainsKey(currentCategoryValue))
                    m_LabelSelectionIndex = Array.FindIndex(m_SpriteLibSelection[currentCategoryValue].entryNames, x => x == currentLabelValue);
                m_PreviousLabelValue = currentLabelValue;
            }

            ApplyModifiedProperty();
            if (m_SpriteSelectorWidget.NeedUpdatePreview())
                this.Repaint();
        }
        
        void ApplyModifiedProperty()
        {
            m_IgnoreNextDeserializeCallback = true;
            serializedObject.ApplyModifiedProperties();
            m_IgnoreNextDeserializeCallback = false;
        }
    }
}
