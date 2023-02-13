using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace UnityEditor.U2D.Animation
{
    [CustomEditor(typeof(Bone))]
    [CanEditMultipleObjects]
    class BoneEditor : Editor
    {
        static class Style
        {
            public static GUIContent boneId = new GUIContent("Bone ID", "The ID of the bone where this GameObject Transform should associate to for SpriteSkin auto rebinding.");
        }
        private SerializedProperty m_GUID;

        void OnEnable()
        {
            m_GUID = serializedObject.FindProperty("m_Guid");
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_GUID, Style.boneId);
            }
        }
    }
}