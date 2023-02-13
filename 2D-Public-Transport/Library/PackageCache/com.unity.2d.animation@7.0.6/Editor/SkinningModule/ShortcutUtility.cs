using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.U2D.Animation
{
    internal static class ShortcutIds
    {
        public const string toggleToolText = "2D/Animation/Toggle Tool Text";
        public const string restoreBindPose = "2D/Animation/Restore Bind Pose";
        public const string toggleCharacterMode = "2D/Animation/Toggle Character Mode";
        public const string previewPose = "2D/Animation/Preview Pose";
        public const string editBone = "2D/Animation/Edit Bone";
        public const string createBone = "2D/Animation/Create Bone";
        public const string splitBone = "2D/Animation/Split Bone";
        public const string autoGeometry = "2D/Animation/Auto Geometry";
        public const string editGeometry = "2D/Animation/Edit Geometry";
        public const string createVertex = "2D/Animation/Create Vertex";
        public const string createEdge = "2D/Animation/Create Edge";
        public const string splitEdge = "2D/Animation/Split Edge";
        public const string autoWeights = "2D/Animation/Auto Weights";
        public const string weightSlider = "2D/Animation/Weight Slider";
        public const string weightBrush = "2D/Animation/Weight Brush";
        public const string boneInfluence = "2D/Animation/Bone Influence";
        public const string spriteInfluence = "2D/Animation/Sprite Influence";
        public const string pastePanelWeights = "2D/Animation/Paste Panel Weights";
        public const string visibilityPanel = "2D/Animation/Visibility Panel";
    }
    
    internal class ShortcutUtility
    {
        public System.Action OnShortcutChanged;
        
        private string[] m_ShortcutIds;
        
        public ShortcutUtility(params string[] shortcutIds)
        {
            m_ShortcutIds = shortcutIds;
            ShortcutManager.instance.shortcutBindingChanged += OnBindingChanged;
            ShortcutManager.instance.activeProfileChanged += OnProfileChanged;
        }

        ~ShortcutUtility()
        {
            ShortcutManager.instance.shortcutBindingChanged -= OnBindingChanged;
            ShortcutManager.instance.activeProfileChanged -= OnProfileChanged;
        }

        private void OnBindingChanged(ShortcutBindingChangedEventArgs args)
        {
            foreach (var id in m_ShortcutIds)
            {
                if (args.shortcutId == id)
                {
                    OnShortcutChanged?.Invoke();
                    break;
                }
            }
        }

        private void OnProfileChanged(ActiveProfileChangedEventArgs args)
        {
            OnShortcutChanged?.Invoke();
        }

        public void AddShortcutToButtonTooltip(VisualElement rootElement, string buttonName, string shortcutBindingId)
        {
            var button = rootElement.Q<Button>(buttonName);
            var binding = ShortcutManager.instance.GetShortcutBinding(shortcutBindingId);
            var bindingString = binding.ToString();
            
            if(!string.IsNullOrEmpty(bindingString))
                button.tooltip += $" ({bindingString})";
        }
    }
}
