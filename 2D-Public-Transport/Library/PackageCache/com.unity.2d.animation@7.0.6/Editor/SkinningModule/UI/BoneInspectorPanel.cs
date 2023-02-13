using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Animation
{
    internal class BoneInspectorPanel : VisualElement
    {
        [Flags]
        internal enum PropertyReadOnly
        {
            None,
            Name = 1,
            Depth = 1 << 2,
            Position = 1 << 3,
            Rotation  = 1 << 4,
            Color = 1 << 5
        }
        
        public class BoneInspectorPanelFactory : UxmlFactory<BoneInspectorPanel, BoneInspectorPanelUxmlTraits> {}
        public class BoneInspectorPanelUxmlTraits : UxmlTraits {}
        public event Action<BoneCache, int> onBoneDepthChanged = (bone, depth) => {};
        public event Action<BoneCache, Vector2> onBonePositionChanged = (bone, position) => {};
        public event Action<BoneCache, float> onBoneRotationChanged = (bone, rotation) => {};
        public event Action<BoneCache, string> onBoneNameChanged = (bone, name) => {};
        public event Action<BoneCache, Color32> onBoneColorChanged = (bone, color) => {};

        private TextField m_BoneNameField;
        private IntegerField m_BoneDepthField;
        private FloatField m_BoneRotationField;
        private Vector2Field m_BonePositionField;
        private ColorField m_BoneColorField;
        
        public string boneName
        {
            get { return m_BoneNameField.value; }
            set { m_BoneNameField.value = value; }
        }

        public BoneCache target { get; set; }
        
        public int boneDepth
        {
            get { return m_BoneDepthField.value; }
            set { m_BoneDepthField.value = value; }
        }

        public Vector2 bonePosition
        {
            get { return m_BonePositionField.value; }
            set { m_BonePositionField.SetValueWithoutNotify(value);}
        }
        
        public float boneRotation
        {
            get { return m_BoneRotationField.value; }
            set { m_BoneRotationField.SetValueWithoutNotify(value);}
        }

        public Color32 boneColor
        {
            get => m_BoneColorField.value;
            set { m_BoneColorField.SetValueWithoutNotify(value);}
        }

        public BoneInspectorPanel()
        {
            styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/BoneInspectorPanelStyle.uss"));

            RegisterCallback<MouseDownEvent>((e) => { e.StopPropagation(); });
            RegisterCallback<MouseUpEvent>((e) => { e.StopPropagation(); });
        }

        public void BindElements()
        {
            m_BoneNameField = this.Q<TextField>("BoneNameField");
            m_BoneDepthField = this.Q<IntegerField>("BoneDepthField");
            m_BoneRotationField = this.Q<FloatField>("BoneRotationField");
            m_BonePositionField = this.Q<Vector2Field>("BonePositionField");
            m_BoneColorField = this.Q<ColorField>("BoneColorField");
            m_BoneNameField.RegisterCallback<FocusOutEvent>(BoneNameFocusChanged);
            m_BoneDepthField.RegisterCallback<FocusOutEvent>(BoneDepthFocusChanged);
            m_BoneRotationField.RegisterValueChangedCallback(evt => onBoneRotationChanged(target, evt.newValue));
            m_BonePositionField.RegisterValueChangedCallback(evt => onBonePositionChanged(target, evt.newValue));
            m_BoneColorField.RegisterValueChangedCallback(evt => onBoneColorChanged(target, evt.newValue));
        }

        private void BoneNameFocusChanged(FocusOutEvent evt)
        {
            onBoneNameChanged(target, boneName);
        }

        private void BoneDepthFocusChanged(FocusOutEvent evt)
        {
            onBoneDepthChanged(target, boneDepth);
        }
        public void HidePanel()
        {
            // We are hidding the panel, sent any unchanged value
            this.SetHiddenFromLayout(true);
            onBoneNameChanged(target, boneName);
            onBoneDepthChanged(target, boneDepth);
        }
        public static BoneInspectorPanel GenerateFromUXML()
        {
            var visualTree = ResourceLoader.Load<VisualTreeAsset>("SkinningModule/BoneInspectorPanel.uxml");
            var clone = visualTree.CloneTree().Q<BoneInspectorPanel>("BoneInspectorPanel");
            clone.LocalizeTextInChildren();
            clone.BindElements();
            return clone;
        }

        public void SetReadOnly(PropertyReadOnly property)
        {
            m_BoneDepthField.SetEnabled(!property.HasFlag(PropertyReadOnly.Depth));
            m_BoneNameField.SetEnabled(!property.HasFlag(PropertyReadOnly.Name));
            m_BonePositionField.SetEnabled(!property.HasFlag(PropertyReadOnly.Position));
            m_BoneRotationField.SetEnabled(!property.HasFlag(PropertyReadOnly.Rotation));
            m_BoneColorField.SetEnabled(!property.HasFlag(PropertyReadOnly.Color));
        }
        
    }
}
