using System;
using UnityEditor.U2D.Layout;
using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal class SkeletonToolView
    {
        private BoneInspectorPanel m_BoneInspectorPanel;

        public event Action<BoneCache, string> onBoneNameChanged = (b, s) => {};
        public event Action<BoneCache, int> onBoneDepthChanged = (b, i) => {};
        public event Action<BoneCache, float> onBoneRotationChanged = (b, i) => {};
        public event Action<BoneCache, Vector2> onBonePositionChanged = (b, i) => {};
        public event Action<BoneCache, Color32> onBoneColorChanged = (b, i) => {};

        public SkeletonToolView()
        {
            m_BoneInspectorPanel = BoneInspectorPanel.GenerateFromUXML();
            m_BoneInspectorPanel.onBoneNameChanged += (b, n) =>  onBoneNameChanged(b, n);
            m_BoneInspectorPanel.onBoneDepthChanged += (b, d) => onBoneDepthChanged(b, d);
            m_BoneInspectorPanel.onBoneRotationChanged += (b, n) =>  onBoneRotationChanged(b, n);
            m_BoneInspectorPanel.onBonePositionChanged += (b, d) => onBonePositionChanged(b, d);
            m_BoneInspectorPanel.onBoneColorChanged += (b, d) => onBoneColorChanged(b, d);
            Hide();
        }
        
        public void Initialize(LayoutOverlay layout)
        {
            layout.rightOverlay.Add(m_BoneInspectorPanel);
        }

        public void Show(BoneCache target, bool isReadOnly)
        {
            m_BoneInspectorPanel.target = target;
            m_BoneInspectorPanel.SetHiddenFromLayout(false);
            var readOnlyProperty = BoneInspectorPanel.PropertyReadOnly.None;
            if (isReadOnly)
                readOnlyProperty = BoneInspectorPanel.PropertyReadOnly.Name |
                                   BoneInspectorPanel.PropertyReadOnly.Depth |
                                   BoneInspectorPanel.PropertyReadOnly.Color;
            m_BoneInspectorPanel.SetReadOnly(readOnlyProperty);
        }

        public BoneCache target => m_BoneInspectorPanel.target;

        public void Hide()
        {
            m_BoneInspectorPanel.HidePanel();
            m_BoneInspectorPanel.target = null;
        }

        public void Update(string name, int depth, Vector2 position, float rotation, Color32 color)
        {
            m_BoneInspectorPanel.boneName = name;
            m_BoneInspectorPanel.boneDepth = depth;
            m_BoneInspectorPanel.bonePosition = position;
            m_BoneInspectorPanel.boneRotation = rotation;
            m_BoneInspectorPanel.boneColor = color;
        }
    }
}
