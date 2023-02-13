using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class WeightToolbar : Toolbar
    {
        private const string k_UxmlPath = "SkinningModule/WeightToolbar.uxml";
        private const string k_ToolbarId = "WeightToolbar";
        
        private const string k_AutoGenerateWeightId = "AutoGenerateWeight";
        private const string k_WeightPainterSliderId = "WeightPainterSlider";
        private const string k_WeightPainterBrushId = "WeightPainterBrush";
        private const string k_BoneInfluenceWidgetId = "BoneInfluenceWidget";
        private const string k_SpriteInfluenceWidgetId = "SpriteInfluenceWidget";
        
        public class CustomUXMLFactor : UxmlFactory<WeightToolbar, UxmlTraits> {}

        public event Action<Tools> SetWeightTool = (mode) => {};
        public SkinningCache skinningCache { get; set; }

        public static WeightToolbar GenerateFromUXML()
        {
            var clone = GetClone(k_UxmlPath, k_ToolbarId) as WeightToolbar;
            clone.BindElements();
            clone.SetupShortcutUtility();
            clone.LocalizeTextInChildren();
            clone.AddShortcutsToToolTips();
            return clone;
        }
        
        public WeightToolbar()
        {
            styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/WeightToolbarStyle.uss"));
        }

        public void EnableBoneInfluenceWidget()
        {
            this.Q<Button>(k_BoneInfluenceWidgetId).SetEnabled(true);
        }

        public void DisableBoneInfluenceWidget()
        {
            this.Q<Button>(k_BoneInfluenceWidgetId).SetEnabled(false);
        }

        private void BindElements()
        {
            var button = this.Q<Button>(k_AutoGenerateWeightId);
            button.clickable.clicked += () => SetWeightTool(Tools.GenerateWeights);

            button = this.Q<Button>(k_WeightPainterSliderId);
            button.clickable.clicked += () => SetWeightTool(Tools.WeightSlider);

            button = this.Q<Button>(k_WeightPainterBrushId);
            button.clickable.clicked += () => SetWeightTool(Tools.WeightBrush);

            button = this.Q<Button>(k_BoneInfluenceWidgetId);
            button.clickable.clicked += () => SetWeightTool(Tools.BoneInfluence);
            
            button = this.Q<Button>(k_SpriteInfluenceWidgetId);
            button.clickable.clicked += () => SetWeightTool(Tools.SpriteInfluence);
        }
        
        private void SetupShortcutUtility()
        {
            m_ShortcutUtility = new ShortcutUtility(ShortcutIds.autoWeights,
                ShortcutIds.weightSlider,
                ShortcutIds.weightBrush,
                ShortcutIds.boneInfluence,
                ShortcutIds.spriteInfluence);
            m_ShortcutUtility.OnShortcutChanged = () =>
            {
                RestoreButtonTooltips(k_UxmlPath, k_ToolbarId);
                AddShortcutsToToolTips();
            };            
        }          

        public void UpdateToggleState()
        {
            //TODO: Make UI not be aware of BaseTool, Cache, etc. Use Tool enum
            var button = this.Q<Button>(k_AutoGenerateWeightId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.GenerateWeights).isActive);            
            
            button = this.Q<Button>(k_WeightPainterSliderId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.WeightSlider).isActive);

            button = this.Q<Button>(k_WeightPainterBrushId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.WeightBrush).isActive);
            
            button = this.Q<Button>(k_BoneInfluenceWidgetId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.BoneInfluence).isActive);
            
            button = this.Q<Button>(k_SpriteInfluenceWidgetId);
            SetButtonChecked(button, skinningCache.GetTool(Tools.SpriteInfluence).isActive);
        }
        
        private void AddShortcutsToToolTips()
        {
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_AutoGenerateWeightId, ShortcutIds.autoWeights);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_WeightPainterSliderId, ShortcutIds.weightSlider);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_WeightPainterBrushId, ShortcutIds.weightBrush);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_BoneInfluenceWidgetId, ShortcutIds.boneInfluence);
            m_ShortcutUtility.AddShortcutToButtonTooltip(this, k_SpriteInfluenceWidgetId, ShortcutIds.spriteInfluence);
        }
    }
}
