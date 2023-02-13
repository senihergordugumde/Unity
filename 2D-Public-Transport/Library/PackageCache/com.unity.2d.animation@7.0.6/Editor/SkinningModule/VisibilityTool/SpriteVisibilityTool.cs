using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal interface ISpriteVisibilityToolModel
    {
        ISpriteVisibilityToolView view { get; }
        CharacterCache character { get; }
        bool previousVisibility { get; set; }
        bool allVisibility { get; set; }
        SkinningMode mode { get; }
        bool hasCharacter { get; }
        UndoScope UndoScope(string description);
        SpriteCache selectedSprite { get; }
    }

    internal interface ISpriteVisibilityToolView
    {
        void Setup();
        void SetSelection(SpriteCache sprite);
        void SetSelectionIds(IList<int> selectedIds);
    }

    internal class SpriteVisibilityToolData : CacheObject
    {
        [SerializeField]
        bool m_AllVisibility = true;
        bool m_PreviousVisibility = true;

        public bool allVisibility
        {
            get { return m_AllVisibility; }
            set { m_PreviousVisibility = m_AllVisibility = value; }
        }

        public bool previousVisibility
        {
            get { return m_PreviousVisibility; }
            set { m_PreviousVisibility = value; }
        }
    }

    internal class SpriteVisibilityToolController
    {
        bool m_UpdateViewOnSelection = true;

        internal interface ISpriteVisibilityItem
        {
            bool visibility { get; set; }
            ICharacterOrder characterOrder { get;}
        }

        internal class SpriteVisibilityGroupItem : ISpriteVisibilityItem
        {
            public CharacterGroupCache group;
            public ISpriteVisibilityItem[] childItems;
            bool ISpriteVisibilityItem.visibility
            {
                get { return group.isVisible; }
                set
                {
                    if (childItems != null)
                    {
                        foreach (var item in childItems)
                            item.visibility = value;
                    }
                    group.isVisible = value;
                }
            }
            
            public ICharacterOrder characterOrder { get { return group; } }
        }

        internal class SpriteVisibilitySpriteItem : ISpriteVisibilityItem
        {
            public CharacterPartCache sprite;
            bool ISpriteVisibilityItem.visibility
            {
                get { return sprite.isVisible; }
                set { sprite.isVisible = value; }
            }
            public ICharacterOrder characterOrder { get { return sprite; } }
        }

        ISpriteVisibilityToolModel m_Model;
        SkinningEvents m_Events;
        public event Action OnAvailabilityChangeListeners = () => {};

        public SpriteVisibilityToolController(ISpriteVisibilityToolModel model, SkinningEvents events)
        {
            m_Model = model;
            m_Events = events;
            m_Events.skinningModeChanged.AddListener(OnViewModeChanged);
        }

        public void Activate()
        {
            m_Events.selectedSpriteChanged.AddListener(OnSpriteSelectedChanged);
            m_Model.view.Setup();
            m_Model.view.SetSelection(m_Model.selectedSprite);
            if (m_Model.previousVisibility != m_Model.allVisibility)
            {
                SetAllCharacterSpriteVisibility();
                m_Model.previousVisibility = m_Model.allVisibility;
            }
        }

        public void Deactivate()
        {
            m_Events.selectedSpriteChanged.RemoveListener(OnSpriteSelectedChanged);
        }

        public void Dispose()
        {
            m_Events.skinningModeChanged.RemoveListener(OnViewModeChanged);
        }

        void OnViewModeChanged(SkinningMode mode)
        {
            OnAvailabilityChangeListeners();
            if (isAvailable && m_Model.previousVisibility != m_Model.allVisibility)
                SetAllCharacterSpriteVisibility();
        }

        private void OnSpriteSelectedChanged(SpriteCache sprite)
        {
            if (m_UpdateViewOnSelection)
                m_Model.view.SetSelection(sprite);
            m_UpdateViewOnSelection = true;
        }

        public bool isAvailable
        {
            get { return m_Model.mode == SkinningMode.Character; }
        }

        void SetAllCharacterSpriteVisibility()
        {
            if (m_Model.hasCharacter)
            {
                using (m_Model.UndoScope(TextContent.spriteVisibility))
                {
                    var parts = m_Model.character.parts;

                    foreach (var part in parts)
                        part.isVisible = m_Model.allVisibility;

                    var groups = m_Model.character.groups;
                    foreach (var group in groups)
                        group.isVisible = m_Model.allVisibility;
                }
            }
        }

        public void SetAllVisibility(bool visibility)
        {
            using (m_Model.UndoScope(TextContent.spriteVisibility))
            {
                m_Model.allVisibility = visibility;
                SetAllCharacterSpriteVisibility();
            }
        }

        public bool GetAllVisibility()
        {
            return m_Model.allVisibility;
        }

        public List<TreeViewItem> BuildTreeView()
        {
            var rows = new List<TreeViewItem>();
            var character = m_Model.character;
            if (character != null)
            {
                var parts = character.parts;
                var groups = character.groups;
                var items = CreateTreeGroup(-1, groups, parts, 0);
                foreach (var item in items)
                    rows.Add(item);
                var groupParts = parts.Where(x => x.parentGroup < 0);
                foreach (var part in groupParts)
                {
                    var ii = CreateTreeViewItem(part, groups, 0);
                    rows.Add(ii);
                }
            }
            rows.Sort((x, y) =>
            {
                var x1 = (TreeViewItemBase<ISpriteVisibilityItem>)x;
                var y1 = (TreeViewItemBase<ISpriteVisibilityItem>)y;
                return SpriteVisibilityItemOrderSort(x1.customData, y1.customData);
            });
            
            return rows;
        }

        int SpriteVisibilityItemOrderSort(ISpriteVisibilityItem x, ISpriteVisibilityItem y)
        {
            return x.characterOrder.order.CompareTo(y.characterOrder.order);
        }
        
        private List<TreeViewItem> CreateTreeGroup(int level, CharacterGroupCache[] groups, CharacterPartCache[] parts, int depth)
        {
            var items = new List<TreeViewItem>();
            for (int j = 0; j < groups.Length; ++j)
            {
                if (groups[j].parentGroup == level)
                {
                    var item = new TreeViewItemBase<ISpriteVisibilityItem>(groups[j].GetInstanceID(), depth, groups[j].name, new SpriteVisibilityGroupItem()
                    {
                        group = groups[j],
                    });
                    items.Add(item);
                    var children = new List<ISpriteVisibilityItem>();
                    // find all sprite that has this group
                    var groupParts = parts.Where(x => x.parentGroup == j);
                    foreach (var part in groupParts)
                    {
                        var ii = CreateTreeViewItem(part, groups, depth + 1);
                        items.Add(ii);
                        var visibilityItem = ii as TreeViewItemBase<ISpriteVisibilityItem>;
                        if (visibilityItem != null)
                            children.Add(visibilityItem.customData);
                    }

                    var childItemes = CreateTreeGroup(j, groups, parts, depth + 1);
                    foreach (var iii in childItemes)
                    {
                        items.Add(iii);
                        var visibilityItem = iii as TreeViewItemBase<ISpriteVisibilityItem>;
                        if (visibilityItem != null)
                            children.Add(visibilityItem.customData);
                    }
                    (item.customData as SpriteVisibilityGroupItem).childItems = children.ToArray();
                }
            }
            return items;
        }

        private TreeViewItem CreateTreeViewItem(CharacterPartCache part, CharacterGroupCache[] groups, int depth)
        {
            var name = part.sprite.name;
            return new TreeViewItemBase<ISpriteVisibilityItem>(part.sprite.GetInstanceID(), depth, name,
                new SpriteVisibilitySpriteItem()
                {
                    sprite = part,
                });
        }

        public bool GetCharacterPartVisibility(TreeViewItem item)
        {
            var i = item as TreeViewItemBase<ISpriteVisibilityItem>;
            if (i != null)
                return i.customData.visibility;
            return false;
        }

        public void SetCharacterPartVisibility(TreeViewItem item, bool visible, bool isolate)
        {
            var i = item as TreeViewItemBase<ISpriteVisibilityItem>;
            if (i != null)
            {
                var characterPart = i.customData;
                var character = m_Model.character;
                using (m_Model.UndoScope(TextContent.spriteVisibility))
                {
                    if (isolate)
                    {
                        foreach (var cpart in character.parts)
                        {
                            cpart.isVisible = visible;
                        }
                        characterPart.visibility = !visible;
                    }
                    else
                    {
                        characterPart.visibility = visible;
                    }
                }
            }
        }

        public void SetSelectedSprite(IList<TreeViewItem> rows, IList<int> selectedIds)
        {
            SpriteCache newSelected = null;
            if (selectedIds.Count > 0)
            {
                var selected = rows.FirstOrDefault(x =>
                {
                    var item = ((TreeViewItemBase<ISpriteVisibilityItem>)x).customData as SpriteVisibilitySpriteItem;
                    if (item != null && item.sprite.sprite.GetInstanceID() == selectedIds[0])
                        return true;
                    return false;
                }) as TreeViewItemBase<ISpriteVisibilityItem>;
                if (selected != null)
                    newSelected = ((SpriteVisibilitySpriteItem)selected.customData).sprite.sprite;
            }

            using (m_Model.UndoScope(TextContent.selectionChange))
            {
                m_UpdateViewOnSelection = false;
                m_Events.selectedSpriteChanged.Invoke(newSelected);
                if (newSelected == null)
                    m_Model.view.SetSelectionIds(selectedIds);
            }
        }

        public int GetTreeViewSelectionID(SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.GetInstanceID();
            return 0;
        }
    }

    internal class SpriteVisibilityTool : IVisibilityTool, ISpriteVisibilityToolModel
    {
        SpriteVisibilityToolView m_View;
        SpriteVisibilityToolController m_Controller;

        private SpriteVisibilityToolData m_Data;
        private SkinningCache m_SkinningCache;
        public SkinningCache skinningCache { get { return m_SkinningCache; } }

        public SpriteVisibilityTool(SkinningCache s)
        {
            m_SkinningCache = s;
            m_Data = skinningCache.CreateCache<SpriteVisibilityToolData>();
            m_Controller = new SpriteVisibilityToolController(this, skinningCache.events);
            m_View = new SpriteVisibilityToolView()
            {
                GetController = () => m_Controller
            };
        }

        public void Setup()
        {}

        public void Dispose()
        {
            m_Controller.Dispose();
        }

        public VisualElement view => m_View;
        public string name => TextContent.sprite;

        public void Activate()
        {
            m_Controller.Activate();
        }

        public void Deactivate()
        {
            m_Controller.Deactivate();
        }

        public bool isAvailable
        {
            get { return m_Controller.isAvailable; }
        }


        public void SetAvailabilityChangeCallback(Action callback)
        {
            m_Controller.OnAvailabilityChangeListeners += callback;
        }

        ISpriteVisibilityToolView ISpriteVisibilityToolModel.view { get {return m_View;} }

        bool ISpriteVisibilityToolModel.hasCharacter { get { return skinningCache.hasCharacter; } }
        SpriteCache ISpriteVisibilityToolModel.selectedSprite { get { return skinningCache.selectedSprite; } }
        CharacterCache ISpriteVisibilityToolModel.character { get { return skinningCache.character; } }
        bool ISpriteVisibilityToolModel.previousVisibility { get { return m_Data.previousVisibility; } set { m_Data.previousVisibility = value; } }
        bool ISpriteVisibilityToolModel.allVisibility { get { return m_Data.allVisibility; } set { m_Data.allVisibility = value; } }
        SkinningMode ISpriteVisibilityToolModel.mode { get { return skinningCache.mode; } }

        UndoScope ISpriteVisibilityToolModel.UndoScope(string description)
        {
            return skinningCache.UndoScope(description);
        }
    }

    internal class SpriteVisibilityToolView : VisibilityToolViewBase, ISpriteVisibilityToolView
    {
        public Func<SpriteVisibilityToolController> GetController = () => null;

        public SpriteVisibilityToolView()
        {
            var columns = new MultiColumnHeaderState.Column[2];
            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = VisibilityTreeViewBase.VisibilityIconStyle.visibilityOnIcon,
                headerTextAlignment = TextAlignment.Center,
                width = 32,
                minWidth = 32,
                maxWidth = 32,
                autoResize = false,
                allowToggleVisibility = true
            };
            columns[1] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(TextContent.name),
                headerTextAlignment = TextAlignment.Center,
                width = 230,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false
            };
            var multiColumnHeaderState = new MultiColumnHeaderState(columns);
            var multiColumnHeader = new VisibilityToolColumnHeader(multiColumnHeaderState)
            {
                GetAllVisibility = InternalGetAllVisibility,
                SetAllVisibility = InternalSetAllVisibility,
                canSort = false,
                height = 20,
                visibilityColumn = 0
            };

            m_TreeView = new SpriteTreeView(m_TreeViewState, multiColumnHeader)
            {
                GetController = InternalGetController
            };
            SetupSearchField();
        }

        SpriteVisibilityToolController InternalGetController()
        {
            return GetController();
        }

        bool InternalGetAllVisibility()
        {
            return GetController().GetAllVisibility();
        }

        void InternalSetAllVisibility(bool visibility)
        {
            GetController().SetAllVisibility(visibility);
        }

        public void Setup()
        {
            ((SpriteTreeView)m_TreeView).Setup();
        }

        public void SetSelection(SpriteCache sprite)
        {
            ((SpriteTreeView)m_TreeView).SetSelection(sprite);
        }

        public void SetSelectionIds(IList<int> selectedIds)
        {
            ((SpriteTreeView)m_TreeView).SetSelectionIds(selectedIds);
        }
    }

    class SpriteTreeView : VisibilityTreeViewBase
    {
        public Func<SpriteVisibilityToolController> GetController = () => null;
        public GUIStyle m_Style;
        private TreeViewItem m_CurrentEdittingItem;

        public SpriteTreeView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
            : base(treeViewState, multiColumnHeader)
        {
            columnIndexForTreeFoldouts = 1;
        }

        void SkinInit()
        {
            if (m_Style == null)
            {
                GUIStyle foldOut = "IN Foldout";
                m_Style = new GUIStyle(foldOut);
                m_Style.stretchWidth = false;
                m_Style.richText = false;
                m_Style.border = new RectOffset(-800, -10, 0, -10);
                m_Style.padding = new RectOffset(11, 16, 2, 2);
                m_Style.fixedWidth = 0;
                m_Style.alignment = TextAnchor.MiddleCenter;
                m_Style.clipping = TextClipping.Clip;
                m_Style.normal.background = foldOut.onFocused.background;
                m_Style.normal.scaledBackgrounds = foldOut.onFocused.scaledBackgrounds;
                m_Style.normal.textColor = foldOut.normal.textColor;
                m_Style.onNormal.background = m_Style.normal.background;
                m_Style.onNormal.scaledBackgrounds = m_Style.normal.scaledBackgrounds;
                m_Style.onNormal.textColor = m_Style.normal.textColor;
                m_Style.onActive.background = m_Style.normal.background;
                m_Style.onActive.scaledBackgrounds = m_Style.normal.scaledBackgrounds;
                m_Style.onActive.textColor = m_Style.normal.textColor;
                m_Style.active.background = m_Style.normal.background;
                m_Style.active.scaledBackgrounds = m_Style.normal.scaledBackgrounds;
                m_Style.active.textColor = m_Style.normal.textColor;
                m_Style.onFocused.background = m_Style.normal.background;
                m_Style.onFocused.scaledBackgrounds = m_Style.normal.scaledBackgrounds;
                m_Style.onFocused.textColor = m_Style.normal.textColor;
                m_Style.focused.background = m_Style.normal.background;
                m_Style.focused.scaledBackgrounds = m_Style.normal.scaledBackgrounds;
                m_Style.focused.textColor = m_Style.normal.textColor;
            }
        }

        public void Setup()
        {
            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                SkinInit();

            base.OnGUI(rect);
        }
        
        void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    DrawVisibilityCell(cellRect, item);
                    break;
                case 1:
                    DrawNameCell(cellRect, item, ref args);
                    break;
            }
        }

        void DrawVisibilityCell(Rect cellRect, TreeViewItem item)
        {
            var style = MultiColumnHeader.DefaultStyles.columnHeaderCenterAligned;
            var characterPartVisibility = GetController().GetCharacterPartVisibility(item);

            EditorGUI.BeginChangeCheck();

            var visible = GUI.Toggle(cellRect, characterPartVisibility, characterPartVisibility ? VisibilityIconStyle.visibilityOnIcon : VisibilityIconStyle.visibilityOffIcon, style);

            if (EditorGUI.EndChangeCheck())
            {
                GetController().SetCharacterPartVisibility(item, visible, Event.current.alt);
            }
        }

        void DrawNameCell(Rect cellRect, TreeViewItem item, ref RowGUIArgs args)
        {
            args.rowRect = cellRect;
            base.RowGUI(args);
        }

        
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            Rect cellRect = GetCellRectForTreeFoldouts(rowRect);
            CenterRectUsingSingleLineHeight(ref cellRect);
            return base.GetRenameRect(cellRect, row, item);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var rows = GetController() != null ? GetController().BuildTreeView() : new List<TreeViewItem>();
            SetupParentsAndChildrenFromDepths(root, rows);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            GetController().SetSelectedSprite(GetRows(), selectedIds);
        }

        public void SetSelectionIds(IList<int> selectedIds)
        {
            SetSelection(selectedIds, TreeViewSelectionOptions.RevealAndFrame);
        }

        public void SetSelection(SpriteCache sprite)
        {
            var id = GetController().GetTreeViewSelectionID(sprite);
            SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
        }
    }
}
