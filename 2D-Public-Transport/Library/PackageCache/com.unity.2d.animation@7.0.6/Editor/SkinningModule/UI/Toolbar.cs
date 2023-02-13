using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D.Common;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class Toolbar : VisualElement
    {
        private const string k_UssPath = "SkinningModule/ToolbarStyle.uss";
        
        public class ToolbarFactory : UxmlFactory<Toolbar, ToolbarUxmlTraits> {}
        public class ToolbarUxmlTraits : UxmlTraits {}

        protected ShortcutUtility m_ShortcutUtility;
        
        protected static Toolbar GetClone(string uxmlPath, string toolbarId)
        {
            var visualTree = ResourceLoader.Load<VisualTreeAsset>(uxmlPath);
            return visualTree.CloneTree().Q<Toolbar>(toolbarId);
        }

        public Toolbar()
        {
            AddToClassList("Toolbar");
            styleSheets.Add(ResourceLoader.Load<StyleSheet>(k_UssPath));
            if (EditorGUIUtility.isProSkin)
                AddToClassList("Dark");
        }

        public void SetButtonChecked(Button toCheck)
        {
            var buttons = this.Query<Button>();
            buttons.ForEach((button) => { button.SetChecked(button == toCheck); });
        }

        protected void SetButtonChecked(Button button, bool check)
        {
            if (button.IsChecked() != check)
            {
                if (check)
                    button.AddToClassList("Checked");
                else
                    button.RemoveFromClassList("Checked");
                button.SetChecked(check);
            }
        }

        public void CollapseToolBar(bool collapse)
        {
            if (collapse)
                AddToClassList("Collapse");
            else
                RemoveFromClassList("Collapse");
        }

        protected void RestoreButtonTooltips(string uxmlPath, string toolbarId)
        {
            var clone = GetClone(uxmlPath, toolbarId);
            var clonedButtons = clone.Query<Button>().ToList();
            var originalButtons = this.Query<Button>().ToList();

            Assert.AreEqual(originalButtons.Count, clonedButtons.Count);
            for (var i = 0; i < clonedButtons.Count; ++i)
            {
                originalButtons[i].tooltip = clonedButtons[i].tooltip;
                originalButtons[i].LocalizeTextInChildren();
            }            
        }
    }
}
