using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace UnityEditor.U2D.Animation
{
    internal class SpriteLibraryDataInspector
    {
        class Style
        {
            public GUIContent duplicateWarningText = EditorGUIUtility.TrTextContent("Duplicate name found or name hash clashes. Please use a different name");
            public GUIContent duplicateWarning;
            public int lineSpacing = 3;
            public int  spriteGridSize = 64;
            public float gridPadding = EditorGUIUtility.standardVerticalSpacing * 2;
            public int gridHeaderSize = 20;
            public int gridFooterSize = 20;
            public float gridHeight;
            public Vector2 gridSize;
            public GUIStyle footerBackground;
            public GUIStyle boxBackground;
            public GUIStyle preButton;
            public GUIStyle headerBackground;
            public GUIStyle gridList;
            public GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to list");
            public GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection or last element from list");
            public GUIContent overrideIcon = EditorGUIUtility.TrIconContent("PrefabOverlayAdded Icon");
            public string newCategoryText = L10n.Tr("New Category");
            public string categoryLabel = L10n.Tr("Category");
            public float categoryElementHeight;
            public float categoryTextFieldHeight;
            public string newEntryText = L10n.Tr("Entry");
            public float categoryLabelWidth = 100;

            public Style()
            {
               gridSize = new Vector2(spriteGridSize + gridPadding * 2,
                   spriteGridSize + lineSpacing + EditorGUIUtility.singleLineHeight + gridPadding * 2);
               duplicateWarning = EditorGUIUtility.TrIconContent("console.warnicon.sml", duplicateWarningText.text);
               gridHeight = gridSize.y +gridPadding + gridFooterSize;
               categoryTextFieldHeight  = EditorGUIUtility.singleLineHeight + lineSpacing;
               categoryElementHeight = gridHeight + categoryTextFieldHeight + gridHeaderSize;
               
            }

            public void InitStyle(Event currentEvent)
            {
                if (footerBackground == null && currentEvent.type == EventType.Repaint)
                {
                    footerBackground = "RL Footer";
                    boxBackground = "RL Background";
                    preButton = "RL FooterButton";
                    headerBackground = "RL Header";
                    gridList = "GridList";
                }
            }
        }
        
        class SpriteCategoryGridState
        {
            public Vector2 scrollPos;
            public int selectedIndex;

            public static SpriteCategoryGridState Default =>
                new SpriteCategoryGridState()
                {
                    scrollPos = Vector2.zero,
                    selectedIndex = 0
                };
        }

        private Style m_Style;
        private SerializedProperty m_Library;
        private List<SpriteCategoryGridState> m_GridStates = new List<SpriteCategoryGridState>();
        private ReorderableList m_LabelReorderableList;

        public SpriteLibraryDataInspector(SerializedObject so,
            SerializedProperty library)
        {
            m_Style = new Style();
            m_Library = library;
            m_LabelReorderableList = new ReorderableList(so, library, true, false,
                true, true);
            m_LabelReorderableList.drawElementCallback = DrawElement;
            m_LabelReorderableList.elementHeight = m_Style.categoryTextFieldHeight;
            m_LabelReorderableList.elementHeightCallback = delegate(int index) { return m_Style.categoryElementHeight; };
            m_LabelReorderableList.onAddCallback = OnAddCallback;
            m_LabelReorderableList.onCanRemoveCallback = OnCanRemoveCallback;
        }

        public static void UpdateLibraryWithNewMainLibrary(SpriteLibraryAsset spriteLib, SerializedProperty library)
        {
            var emptyStringArray = new string[0];
            var categories = spriteLib != null ? spriteLib.GetCategoryNames() : emptyStringArray;
            
            // populate new primary
            int newCatgoryIndex = 0;
            foreach (var newCategory in categories)
            {
                SerializedProperty existingCategory = null;
                if (library.arraySize > 0)
                {
                    var cat = library.GetArrayElementAtIndex(0);
                    for (int i = 0; i < library.arraySize; ++i)
                    {
                        if (cat.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue == newCategory)
                        {
                            existingCategory = cat;
                            if(i != newCatgoryIndex)
                                library.MoveArrayElement(i, newCatgoryIndex);
                            break;
                        }
                        cat.Next(false);
                    }
                }

                if (existingCategory != null)
                {
                    if(!existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue)
                        existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue = true;
                }
                else
                {
                    library.InsertArrayElementAtIndex(newCatgoryIndex);
                    existingCategory = library.GetArrayElementAtIndex(newCatgoryIndex);
                    SetPropertyName(existingCategory, newCategory);
                    existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue = true;
                    existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntryCount).intValue = 0;
                    existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries).arraySize = 0;
                }
                newCatgoryIndex++;
                
                var newEntries = spriteLib.GetCategoryLabelNames(newCategory);
                var entries = existingCategory.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries);
                int newEntryIndex = 0;
                foreach (var newEntry in newEntries)
                {
                    SerializedProperty cacheEntry = null;
                    if (entries.arraySize > 0)
                    {
                        var ent = entries.GetArrayElementAtIndex(0);
                        for (int j = 0; j < entries.arraySize; ++j)
                        {
                            if (ent.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue == newEntry)
                            {
                                cacheEntry = ent;
                                if(j != newEntryIndex)
                                    entries.MoveArrayElement(j, newEntryIndex);
                                break;
                            }
                            ent.Next(false);
                        }
                    }
                    var mainSprite = spriteLib.GetSprite(newCategory, newEntry);
                    if (cacheEntry == null)
                    {
                        entries.InsertArrayElementAtIndex(newEntryIndex);
                        cacheEntry = entries.GetArrayElementAtIndex(newEntryIndex);
                        SetPropertyName(cacheEntry, newEntry);
                        cacheEntry.FindPropertyRelative(SpriteLibraryPropertyString.spriteOverride)
                            .objectReferenceValue = mainSprite;
                    }
                    
                    ++newEntryIndex;
                    if(!cacheEntry.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue)
                        cacheEntry.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue = true;
                    if(cacheEntry.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue != mainSprite)
                        cacheEntry.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue = mainSprite;
                }
            }
                
            // Remove any library or entry that is not in primary and not overridden
            for (int i = 0; i < library.arraySize; ++i)
            {
                var categoryProperty = library.GetArrayElementAtIndex(i);
                var categoryEntriesProperty = categoryProperty.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries);
                var categoryFromMainProperty = categoryProperty.FindPropertyRelative(SpriteLibraryPropertyString.fromMain);
                
                var categoryName = categoryProperty.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
                var categoryInPrimary = categories.Contains(categoryName);
                var entriesInPrimary = categoryInPrimary ? spriteLib.GetCategoryLabelNames(categoryName) : emptyStringArray;

                var categoryOverride = 0;
                for (int j = 0; j < categoryEntriesProperty.arraySize; ++j)
                {
                    var entry = categoryEntriesProperty.GetArrayElementAtIndex(j);
                    var entryName = entry.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
                    var entryInPrimary = entriesInPrimary.Contains(entryName);
                    var entryFromMainProperty = entry.FindPropertyRelative(SpriteLibraryPropertyString.fromMain);
                    var overrideSpriteProperty = entry.FindPropertyRelative(SpriteLibraryPropertyString.spriteOverride);
                    var spriteProperty = entry.FindPropertyRelative(SpriteLibraryPropertyString.sprite);
                    if (!entryInPrimary)
                    {
                        // Entry no longer in new primary.
                        // Check for override and set it to us
                        if (entryFromMainProperty.boolValue)
                        {
                            if (overrideSpriteProperty.objectReferenceValue == spriteProperty.objectReferenceValue)
                            {
                                categoryEntriesProperty.DeleteArrayElementAtIndex(j);
                                --j;
                                continue;
                            }                            
                        }


                        if(entryFromMainProperty.boolValue)
                            entryFromMainProperty.boolValue = false;
                        if(spriteProperty.objectReferenceValue != overrideSpriteProperty.objectReferenceValue)
                            spriteProperty.objectReferenceValue = overrideSpriteProperty.objectReferenceValue;
                        ++categoryOverride;
                    }
                    else
                    {
                        // Check if sprite has been override
                        if(spriteProperty.objectReferenceValue != overrideSpriteProperty.objectReferenceValue)
                            ++categoryOverride;    
                    }
                    
                }

                if (!categoryInPrimary && categoryEntriesProperty.arraySize == 0 && categoryFromMainProperty.boolValue)
                {
                    library.DeleteArrayElementAtIndex(i);
                    --i;
                    continue;
                }
                // since there is override, and we removed the main. This category now
                // belows to the library
                if (!categoryInPrimary)
                {
                    if(categoryFromMainProperty.boolValue)
                        categoryFromMainProperty.boolValue = false;
                }
                else
                {
                    if(categoryProperty.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntryCount).intValue != categoryOverride)
                        categoryProperty.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntryCount).intValue = categoryOverride;   
                }
            }
        }
        
        public void OnGUI()
        {
            m_Style.InitStyle(Event.current);
            m_LabelReorderableList.DoLayoutList();
            HandleCategoryCreateSpriteDragAndDrop();
        }

        void HandleCategoryCreateSpriteDragAndDrop()
        {
            bool dragAccepted = false;
            switch (Event.current.type)
            {
                case EventType.DragPerform:
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Sprite || obj is Texture2D)
                        {
                            dragAccepted = true;
                        }
                    }

                    if (dragAccepted)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is Sprite)
                            {
                                var category = AddCategory(obj.name, false);
                                AddSpriteToCategory(category, (Sprite)obj);
                            }
                            else if (obj is Texture2D)
                            {
                                var sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj))
                                    .Where(x => x is Sprite);
                                foreach (var s in sprites)
                                {
                                    var category = AddCategory(s.name, false);   
                                    AddSpriteToCategory(category, (Sprite)s);
                                }
                            }
                        }
                    }
                    Event.current.Use();
                    break;
                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.Count(x => x is Sprite || x is Texture2D) > 0)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    else
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    Event.current.Use();
                    break;
            }
        }

        SerializedProperty IsCategoryNameInUsed(string name)
        {
            if (m_Library.arraySize == 0)
                return null;
            var nameHash = SpriteLibraryAsset.GetStringHash(name);
            var sp = m_Library.GetArrayElementAtIndex(0);
            for (int i = 0; i < m_Library.arraySize; ++i)
            {
                var nameProperty = sp.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
                var hash = SpriteLibraryAsset.GetStringHash(nameProperty);
                if (nameProperty == name || nameHash == hash)
                    return sp;
                sp.Next(false);
            }

            return null;
        }
        
        void DrawElement(Rect rect, int index, bool selected, bool focused)
        {
            DrawCategory(rect, index);
        }

        void DrawCategory(Rect rect, int index)
        {
            var categorySerializedProperty = m_Library.GetArrayElementAtIndex(index);

            var catRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var vaRect = new Rect(rect.x, rect.y + m_Style.categoryTextFieldHeight, rect.width, EditorGUIUtility.singleLineHeight);

            var categoryName = categorySerializedProperty.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
            var fromMain = categorySerializedProperty.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue;
            using (new EditorGUI.DisabledScope(fromMain))
            {
                EditorGUI.BeginChangeCheck();
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = m_Style.categoryLabelWidth;
                var newCatName = EditorGUI.DelayedTextField(catRect, m_Style.categoryLabel, categoryName);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                if (EditorGUI.EndChangeCheck())
                {
                    newCatName = newCatName.Trim();
                    if (categoryName != newCatName)
                    {
                        // Check if this nameLabel is already taken
                        if (IsCategoryNameInUsed(newCatName) == null)
                            SetPropertyName(categorySerializedProperty, newCatName);
                        else
                            Debug.LogWarning(m_Style.duplicateWarningText.text);
                    }
                }
            }
            
            while(m_GridStates.Count <= index)
                m_GridStates.Add(new SpriteCategoryGridState());
            
            DrawCategorySpriteListHeader(vaRect, m_Style.newEntryText);
            vaRect.y += vaRect.height;
            vaRect.height = m_Style.gridHeight;
            DrawCategorySpriteList(vaRect, index, categorySerializedProperty);

            HandleSpriteDragAndDropToCategory(vaRect, categorySerializedProperty);            
        }

        void OnAddCallback(ReorderableList list)
        {
            AddCategory(m_Style.newCategoryText, true);
        }

        SerializedProperty AddCategory(string categoryName, bool createNew)
        {
            var intendedCategoryName = categoryName;
            int catNameIncrement = 1;
            while (true)
            {
                var catOverride = IsCategoryNameInUsed(categoryName);
                if (catOverride != null)
                {
                    if (!createNew)
                        return catOverride;
                    categoryName = string.Format("{0} {1}", intendedCategoryName, catNameIncrement++);   
                }
                else
                    break;
            }

            var oldSize = m_Library.arraySize;
            m_Library.arraySize += 1;
            var sp = m_Library.GetArrayElementAtIndex(oldSize);
            SetPropertyName(sp, categoryName);
            sp.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue = false;
            sp.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries).arraySize = 0;
            return sp;
        }
        
        bool HandleSpriteDragAndDropToCategory(Rect rect, SerializedProperty category)
        {
            bool dragAccepted = false;
            if (rect.Contains(Event.current.mousePosition))
            {
                switch (Event.current.type)
                {
                    case EventType.DragPerform:
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is Sprite)
                            {
                                AddSpriteToCategory(category, (Sprite)obj);
                                dragAccepted = true;
                            }
                        }

                        if (dragAccepted)
                        {
                            DragAndDrop.AcceptDrag();
                        }
                        Event.current.Use();
                        break;
                    case EventType.DragUpdated:
                        if (DragAndDrop.objectReferences.Count(x => x is Sprite) > 0)
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        else
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        Event.current.Use();
                        break;
                }
            }

            return dragAccepted;
        }
        
        static void GetRowColumnCount(float drawWidth, float size, int contentCount, out int column, out int row, out float columnf)
        {
            columnf = (drawWidth) / size;
            column = (int)Mathf.Floor(columnf);
            if (column == 0)
                row = 0;
            else
                row = (int)Mathf.Ceil((contentCount + column-1) / column);
        }

        void DrawCategorySpriteList(Rect rect, int index, SerializedProperty category)
        {
            var spriteListProp = category.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries);
            var footerRect = rect;
            var gridState = m_GridStates[index];
            gridState.selectedIndex = (gridState.selectedIndex > spriteListProp.arraySize) ? 0 : gridState.selectedIndex;
            rect.height -= m_Style.gridFooterSize;
            if(Event.current.type == EventType.Repaint)
                m_Style.boxBackground.Draw(rect, "", false, false, false, false);
            
            rect.x += m_Style.gridPadding;
            rect.y += m_Style.gridPadding;
            rect.width -= m_Style.gridPadding * 2;
            rect.height -= m_Style.gridPadding * 2;

            float columnF;
            int columnCount, rowCount;
            GetRowColumnCount(rect.width, m_Style.gridSize.x, spriteListProp.arraySize, out columnCount, out rowCount, out columnF);
            bool canRemoveSelectedEntry = true;
            bool selectedEntryIsOverwrite = false;
            
            var overrideCountProperty = category.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntryCount);
            if (spriteListProp.arraySize <= 0 && overrideCountProperty.intValue != 0)
                overrideCountProperty.intValue = 0;
            if (columnCount > 0 && rowCount > 0)
            {
                var spriteOverwrite = 0;
                var scrollViewRect = new Rect(rect.x, rect.y, columnCount * m_Style.gridSize.x,
                    rowCount * m_Style.gridSize.y);
                if (rowCount >= 2)
                    gridState.scrollPos = GUI.BeginScrollView(new Rect(rect.x, rect.y, rect.width, rect.height),gridState.scrollPos, scrollViewRect, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);

                var viewPortMinY = rect.y + gridState.scrollPos.y;
                var viewPortMaxY = rect.y + rect.height + gridState.scrollPos.y;
                
                var spriteObjectFieldRect = new Rect(rect.x, rect.y, m_Style.spriteGridSize, m_Style.spriteGridSize);
                var labelTextfieldRect =  new Rect(rect.x, rect.y + m_Style.spriteGridSize + m_Style.lineSpacing, m_Style.spriteGridSize, EditorGUIUtility.singleLineHeight);
                var backgroundSelectedRect = new Rect(rect.x, rect.y, m_Style.gridSize.x, m_Style.gridSize.y);
                for (int i = 0, row = 0, column = 0; i < spriteListProp.arraySize; ++i, ++column)
                {
                    if (column >= columnCount)
                    {
                        column = 0;
                        ++row;
                    }
                    
                    backgroundSelectedRect.x = column * m_Style.gridSize.x + rect.x;
                    backgroundSelectedRect.y = row * m_Style.gridSize.y + rect.y;
                    spriteObjectFieldRect.x = column * m_Style.gridSize.x + rect.x + m_Style.gridPadding;
                    spriteObjectFieldRect.y = row * m_Style.gridSize.y + rect.y + m_Style.gridPadding;
                    labelTextfieldRect.x = column * m_Style.gridSize.x +rect.x + m_Style.gridPadding;
                    labelTextfieldRect.y = row * m_Style.gridSize.y + m_Style.spriteGridSize +rect.y + m_Style.gridPadding;
                    
                    var element = spriteListProp.GetArrayElementAtIndex(i);

                    var spriteProperty = element.FindPropertyRelative(SpriteLibraryPropertyString.sprite);
                    var spriteOverrideProperty = element.FindPropertyRelative(SpriteLibraryPropertyString.spriteOverride);
                    var spriteOverride = spriteProperty.objectReferenceValue != spriteOverrideProperty.objectReferenceValue;
                    var entryFromMain = element.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue;
                    
                    if (!entryFromMain || spriteOverride)
                        ++spriteOverwrite;
                    
                    if(!IsLabelVisible(viewPortMinY, viewPortMaxY, backgroundSelectedRect))
                        continue;

                    DrawLabel(i, 
                        spriteListProp, 
                        spriteOverrideProperty,
                        gridState, 
                        spriteOverride,
                        entryFromMain,
                        backgroundSelectedRect,
                        spriteObjectFieldRect,
                        labelTextfieldRect,
                        ref canRemoveSelectedEntry,
                        ref selectedEntryIsOverwrite);
                } 
                
                if (overrideCountProperty.intValue != spriteOverwrite)
                {
                    overrideCountProperty.intValue = spriteOverwrite;
                    GUI.changed = true;
                }                
                if (rowCount >= 2)
                    GUI.EndScrollView();
            }
            
            
            footerRect = new Rect(footerRect.x, footerRect.y + footerRect.height - m_Style.gridFooterSize, footerRect.width, m_Style.gridFooterSize);

            Action<SerializedProperty , SpriteCategoryGridState> removeCallback = DeleteSpriteEntry;
            if (selectedEntryIsOverwrite)
                removeCallback = DeleteSpriteEntryOverride;
            DrawCategorySpriteListFooter(footerRect, category, gridState,canRemoveSelectedEntry, removeCallback);
        }

        static bool IsLabelVisible(float viewPortMinY, float viewPortMaxY, Rect labelRect)
        {
            return (labelRect.y + labelRect.height) >= viewPortMinY && labelRect.y <= viewPortMaxY;
        }

        void DrawLabel(int index, 
            SerializedProperty spriteListProp, 
            SerializedProperty spriteOverrideProperty,
            SpriteCategoryGridState gridState,
            bool spriteOverride,
            bool entryFromMain,
            Rect backgroundSelectedRect,
            Rect spriteObjectFieldRect,
            Rect labelTextfieldRect,
            ref bool canRemoveSelectedEntry,
            ref bool selectedEntryIsOverwrite)
        {
            var element = spriteListProp.GetArrayElementAtIndex(index);

            if (gridState.selectedIndex == index)
            {
                canRemoveSelectedEntry = entryFromMain && spriteOverride || !entryFromMain;
                selectedEntryIsOverwrite = entryFromMain && spriteOverride;
                if(Event.current.type == EventType.Repaint)
                    m_Style.gridList.Draw(backgroundSelectedRect, true, true, true, false);
            }
            spriteOverrideProperty.objectReferenceValue = EditorGUI.ObjectField(spriteObjectFieldRect, spriteOverrideProperty.objectReferenceValue, typeof(Sprite), false) as Sprite;
            if (Event.current.type == EventType.MouseUp &&
                backgroundSelectedRect.Contains(Event.current.mousePosition))
            {
                gridState.selectedIndex = index;
            }

            if (!entryFromMain || spriteOverride)
            {
                var overrideIconRect = spriteObjectFieldRect;
                overrideIconRect.x -= 12;
                overrideIconRect.y -= 12;
                overrideIconRect.width = 20;
                overrideIconRect.height = 20;
                GUI.Label(overrideIconRect, m_Style.overrideIcon);
            }

            //disable m_Name editing if the entry is from main
            using (new EditorGUI.DisabledScope(entryFromMain))
            {
                EditorGUI.BeginChangeCheck();
                var oldName = element.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
                if (string.IsNullOrEmpty(oldName) && spriteOverrideProperty.objectReferenceValue != null && entryFromMain)
                {
                    oldName = spriteOverrideProperty.name;
                    SetPropertyName(element, oldName);
                }
                var nameRect = labelTextfieldRect;
                bool nameDuplicate = IsEntryNameUsed(oldName, spriteListProp, 1);
                if (nameDuplicate)
                {
                    nameRect.width -= 20;
                }
            
                var newName = EditorGUI.DelayedTextField(
                    nameRect,
                    GUIContent.none, 
                    oldName);

                if (nameDuplicate)
                {
                    nameRect.x += nameRect.width;
                    nameRect.width = 20;
                    GUI.Label(nameRect, m_Style.duplicateWarning);
                }
                if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(newName))
                {
                    newName = newName.Trim();
                    SetPropertyName(element, newName);
                }   
            }            
        }

        bool IsEntryNameUsed(string name, SerializedProperty spriteList, int duplicateAllow)
        {
            if (spriteList.arraySize == 0)
                return false;
            var sp = spriteList.GetArrayElementAtIndex(0);
            var nameHash = SpriteLibraryAsset.GetStringHash(name);
            int count = 0;
            for (int i = 0; i < spriteList.arraySize; ++i, sp.Next(false))
            {
                var stringValue = sp.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue;
                var hash = SpriteLibraryAsset.GetStringHash(stringValue);
                if (stringValue == name || hash == nameHash)
                {
                    ++count;
                    if(count > duplicateAllow)
                        return true;
                }
            }

            return false;
        }
        
        static void DeleteSpriteEntryOverride(SerializedProperty spriteList, SpriteCategoryGridState gridState)
        {
            var sp = spriteList.GetArrayElementAtIndex(gridState.selectedIndex);
            sp.FindPropertyRelative(SpriteLibraryPropertyString.spriteOverride).objectReferenceValue = sp.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue;
        }
        
        static void DeleteSpriteEntry(SerializedProperty spriteList, SpriteCategoryGridState gridState)
        {
            spriteList.DeleteArrayElementAtIndex(gridState.selectedIndex);
            var count = spriteList.arraySize;
            gridState.selectedIndex = (gridState.selectedIndex >= count) ? count - 1 : gridState.selectedIndex;
        }
        
        void DrawCategorySpriteListHeader(Rect rect, string text)
        {
            if (Event.current.type == UnityEngine.EventType.Repaint)
                m_Style.headerBackground.Draw(rect, false, false, false, false);
            rect.x += 10;
            EditorGUI.LabelField(rect, text);
        }
        
        void DrawCategorySpriteListFooter(Rect rect, SerializedProperty category, SpriteCategoryGridState gridState, bool canRemove, Action<SerializedProperty, SpriteCategoryGridState> onRemove)
        {
            float num = rect.xMax - 10f;
            float x = num - 8f;
            x -= 25f;
            x -= 25f;
            rect = new Rect(x, rect.y, num - x, rect.height);
            Rect rect1 = new Rect(x + 4f, rect.y, 25f, 16f);
            Rect position = new Rect(num - 29f, rect.y, 25f, 16f);

            
            if (Event.current.type == UnityEngine.EventType.Repaint)
                m_Style.footerBackground.Draw(rect, false, false, false, false);
            
            if (GUI.Button(rect1, m_Style.iconToolbarPlus, m_Style.preButton)) 
                AddSpriteToCategory(category, null);

            var spriteList = category.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries);
            using (new EditorGUI.DisabledScope(!canRemove || gridState.selectedIndex < 0 || spriteList.arraySize <= gridState.selectedIndex))
            {
                if (GUI.Button(position, m_Style.iconToolbarMinus, m_Style.preButton))
                    onRemove(spriteList, gridState);
            }
        }

        string GetUniqueEntryName(SerializedProperty list,  string name)
        {
            var newName = name;
            var count = 0;
            while (IsEntryNameUsed(newName, list, 0))
            {
                newName = string.Format("{0}_{1}", name, count);
                ++count;
            }
            return newName;
        }
        
        void AddSpriteToCategory(SerializedProperty category, Sprite sprite)
        {
            var name = sprite == null ? m_Style.newEntryText : sprite.name;
            var spriteList = category.FindPropertyRelative(SpriteLibraryPropertyString.overrideEntries);
            name = GetUniqueEntryName(spriteList, name);
            spriteList.arraySize += 1;
            var sp = spriteList.GetArrayElementAtIndex(spriteList.arraySize - 1);
            SetPropertyName(sp, name);
            sp.FindPropertyRelative(SpriteLibraryPropertyString.sprite).objectReferenceValue = sprite;
            sp.FindPropertyRelative(SpriteLibraryPropertyString.spriteOverride).objectReferenceValue = sprite;
            sp.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue = false;
        }

        static void SetPropertyName(SerializedProperty sp, string newName)
        {
            sp.FindPropertyRelative(SpriteLibraryPropertyString.name).stringValue = newName;
            sp.FindPropertyRelative(SpriteLibraryPropertyString.hash).intValue = SpriteLibraryAsset.GetStringHash(newName);
        }
        
        bool OnCanRemoveCallback(ReorderableList list)
        {
            bool canDelete = true;
            if (list.index >= 0 && list.index < list.count)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(list.index);
                canDelete = !item.FindPropertyRelative(SpriteLibraryPropertyString.fromMain).boolValue;
            }

            return canDelete;
        }
    }
}
