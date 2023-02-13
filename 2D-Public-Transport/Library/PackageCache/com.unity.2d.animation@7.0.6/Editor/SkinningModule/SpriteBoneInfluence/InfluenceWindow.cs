using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal interface IInfluenceWindow
    {
        void UpdateList(List<TransformCache> transformsList);
        void UpdateSelection(IEnumerable<TransformCache> newSelection);
        void ToggleAddButton(bool enabled);
        void ToggleRemoveButton(bool enabled);
        string headerText { get; set; }
        string listLabelText { set; }
        void SetHiddenFromLayout(bool hide);
        void SetTooltips(string addButtonTooltip, string removeButtonTooltip);
        bool visible { get; }
        
        event Action onAddElement;
        event Action onRemoveElement;
        event Action<IEnumerable<TransformCache>> onReordered;
        event Action<IEnumerable<object>> onSelectionChanged;
    }
    
    internal class InfluenceWindow : VisualElement, IInfluenceWindow
    {
        public class CustomUxmlFactory : UxmlFactory<InfluenceWindow, UxmlTraits> { }

        public event Action onAddElement = () => {};
        public event Action onRemoveElement = () => {};
        public event Action<IEnumerable<TransformCache>> onReordered = _ => {};
        public event Action<IEnumerable<object>> onSelectionChanged = _ => {};

        UnityEngine.UIElements.PopupWindow m_HeaderLabel;
        
        Label m_ListLabel;
        
        IEnumerable<TransformCache> m_Selection;
        
        ListView m_ListView;
        List<TransformCache> m_Influences = new List<TransformCache>();
        
        Button m_AddButton;
        Button m_RemoveButton;
        bool m_IgnoreSelectionChange = false;

        public string headerText
        {
            get => m_HeaderLabel.text;
            set => m_HeaderLabel.text = value;
        }

        public string listLabelText
        {
            get => m_ListLabel.text;
            set => m_ListLabel.text = value;
        }

        public void SetTooltips(string addButtonTooltip, string removeButtonTooltip)
        {
            m_AddButton.tooltip = addButtonTooltip;
            m_RemoveButton.tooltip = removeButtonTooltip;
        }

        internal static InfluenceWindow CreateFromUxml()
        {
            var visualTree = ResourceLoader.Load<VisualTreeAsset>("SkinningModule/InfluenceWindow.uxml");
            var ve = (InfluenceWindow)visualTree.CloneTree().Q("InfluenceWindow"); 
            ve.styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/InfluenceWindowStyle.uss"));
            if (EditorGUIUtility.isProSkin)
                ve.AddToClassList("Dark");
            ve.LocalizeTextInChildren();
            ve.BindElements();
            return ve;
        }

        public void SetListReorderable(bool reorderable)
        {
            m_ListView.reorderable = reorderable;
        }

        public void SetAllowMultiselect(bool allowMultiselect)
        {
            m_ListView.selectionType = allowMultiselect ? SelectionType.Multiple : SelectionType.Single;
        }

        private void BindElements()
        {
            m_HeaderLabel = this.Q<UnityEngine.UIElements.PopupWindow>();
            m_ListLabel = this.Q<Label>();

            m_ListView = this.Q<ListView>();
            m_ListView.selectionType = SelectionType.Multiple;
            m_ListView.itemsSource = m_Influences;
            m_ListView.makeItem = () =>
            {
                var label = new Label()
                {
                    name = "ListRow"
                };
                return label;
            };
            m_ListView.bindItem = (e, index) =>
            {
                if (m_Influences[index] == null)
                    return;

                ((Label)e).text = m_Influences[index].name;
                if (index % 2 == 0)
                {
                    e.RemoveFromClassList("ListRowOddColor");
                    e.AddToClassList("ListRowEvenColor");
                }
                else
                {
                    e.RemoveFromClassList("ListRowEvenColor");
                    e.AddToClassList("ListRowOddColor");
                }
            };

            m_ListView.onSelectionChange += OnListViewSelectionChanged;
            m_AddButton = this.Q<Button>("AddButton");
            m_AddButton.clickable.clicked += () =>
            {
                onAddElement.Invoke();
            };
            m_RemoveButton = this.Q<Button>("RemoveButton");
            m_RemoveButton.clickable.clicked += () =>
            {
                onRemoveElement.Invoke();
            };
            
            RegisterCallback<DragPerformEvent>(x => onReordered(m_Influences) );
        }

        public void UpdateList(List<TransformCache> transformsList)
        {
            m_Influences = transformsList;
            m_ListView.itemsSource = m_Influences;
            m_ListView.Rebuild();
        }

        public void UpdateSelection(IEnumerable<TransformCache> newSelection)
        {
            m_Selection = newSelection;
            
            m_IgnoreSelectionChange = true;
            m_ListView.ClearSelection();
            foreach (var selection in newSelection)
            {
                var index = m_Influences.IndexOf(selection);
                if(index >= 0)
                    m_ListView.AddToSelection(index);
            }

            m_IgnoreSelectionChange = false;
        }
        
        private void OnListViewSelectionChanged(IEnumerable<object> newSelection)
        {
            if (m_IgnoreSelectionChange)
                return;

            onSelectionChanged(newSelection);
        }

        public void ToggleAddButton(bool enabled)
        {
            m_AddButton.SetEnabled(enabled);
        }

        public void ToggleRemoveButton(bool enabled)
        {
            m_RemoveButton.SetEnabled(enabled && m_ListView.selectedIndex >= 0);
        }

        void IInfluenceWindow.SetHiddenFromLayout(bool hide)
        {
            this.SetHiddenFromLayout(hide);
        }
    }
}
