using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace UnityEditor.U2D.Sprites
{
    internal class SpriteEditorWindowSettings : SettingsProvider
    {
        public const string kSettingsUniqueKey = "UnityEditor.U2D.Sprites/SpriteEditorWindow";
        public const string kShowRevertConfirmation = kSettingsUniqueKey + "RevertConfirmation";
        public const string kShowApplyConfirmation = kSettingsUniqueKey + "ApplyConfirmation";
        public static readonly GUIContent kShowRevertConfirmationLabel = EditorGUIUtility.TrTextContent("Show Revert Confirmation");
        public static readonly GUIContent kShowApplyConfirmationLabel = EditorGUIUtility.TrTextContent("Show Apply Confirmation");

        public SpriteEditorWindowSettings() : base("Preferences/2D/Sprite Editor Window", SettingsScope.User)
        {
            guiHandler = OnGUI;
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SpriteEditorWindowSettings()
            {
                guiHandler = SettingsGUI
            };
        }

        private static void SettingsGUI(string searchContext)
        {
            showApplyConfirmation = EditorGUILayout.Toggle(kShowApplyConfirmationLabel, showApplyConfirmation);
            showRevertConfirmation = EditorGUILayout.Toggle(kShowRevertConfirmationLabel, showRevertConfirmation);
        }

        public static bool showRevertConfirmation
        {
            get { return EditorPrefs.GetBool(kShowRevertConfirmation, false); }
            set { EditorPrefs.SetBool(kShowRevertConfirmation, value); }
        }

        public static bool showApplyConfirmation
        {
            get { return EditorPrefs.GetBool(kShowApplyConfirmation, false); }
            set { EditorPrefs.SetBool(kShowApplyConfirmation, value); }
        }
    }
}
