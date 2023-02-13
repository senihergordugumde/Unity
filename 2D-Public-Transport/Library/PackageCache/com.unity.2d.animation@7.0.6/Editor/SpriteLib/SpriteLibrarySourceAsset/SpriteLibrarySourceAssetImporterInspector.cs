using System;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.U2D.Animation;
using Object = UnityEngine.Object;

namespace UnityEditor.U2D.Animation
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpriteLibrarySourceAssetImporter))]
    internal class SpriteLibrarySourceAssetImporterInspector : ScriptedImporterEditor
    {
        static class Style
        {
            public static readonly GUIContent mainAssetLabel = new("Main Library");
            public static readonly string multiEditWarning = L10n.Tr("It's not possible to edit content of multiple Sprite Library Assets at the same time.");
        }        
        
        SerializedProperty m_PrimaryLibraryGUID;
        SerializedProperty m_Library;
        SpriteLibraryAsset m_MainSpriteLibraryAsset;
        SpriteLibraryDataInspector m_SpriteLibraryDataInspector;
        
        public override bool showImportedObject => false;
        protected override Type extraDataType => typeof(SpriteLibrarySourceAsset);
        
        protected override bool ShouldHideOpenButton() => true;

        public override void OnEnable()
        {
            base.OnEnable();
            m_PrimaryLibraryGUID = extraDataSerializedObject.FindProperty(SpriteLibrarySourceAssetPropertyString.primaryLibraryGUID);
            m_Library = extraDataSerializedObject.FindProperty(SpriteLibrarySourceAssetPropertyString.library);
            m_SpriteLibraryDataInspector = new SpriteLibraryDataInspector(extraDataSerializedObject, m_Library);
        }

        protected override void InitializeExtraDataInstance(Object extraTarget, int targetIndex)
        {
            var assetPath = ((AssetImporter)targets[targetIndex]).assetPath;
            var savedAsset = SpriteLibrarySourceAssetImporter.LoadSpriteLibrarySourceAsset(assetPath);
            if (savedAsset != null)
            {
                // Add entries from Main Library Asset.
                if (!SpriteLibrarySourceAssetImporter.HasValidMainLibrary(savedAsset, assetPath))
                    savedAsset.SetPrimaryLibraryGUID(string.Empty);
                SpriteLibrarySourceAssetImporter.UpdateSpriteLibrarySourceAssetLibraryWithMainAsset(savedAsset);
                (extraTarget as SpriteLibrarySourceAsset).InitializeWithAsset(savedAsset);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            extraDataSerializedObject.Update();
            DoMainAssetGUI();
            DoLibraryGUI();
            serializedObject.ApplyModifiedProperties();
            extraDataSerializedObject.ApplyModifiedProperties();
            
            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();
            for (var i = 0; i < targets.Length; i++)
            {
                var path = ((AssetImporter)targets[i]).assetPath;
                var sourceAsset = (SpriteLibrarySourceAsset)extraDataTargets[i];
                var savedAsset = SpriteLibrarySourceAssetImporter.LoadSpriteLibrarySourceAsset(path);
                savedAsset.InitializeWithAsset(sourceAsset);

                // Remove entries that come from Main Library Asset before saving.
                var savedLibrarySerializedObject = new SerializedObject(savedAsset);
                SpriteLibraryDataInspector.UpdateLibraryWithNewMainLibrary(null, savedLibrarySerializedObject.FindProperty(SpriteLibrarySourceAssetPropertyString.library));
                if (savedLibrarySerializedObject.hasModifiedProperties)
                    savedLibrarySerializedObject.ApplyModifiedPropertiesWithoutUndo();
                // Save asset to disk.
                SpriteLibrarySourceAssetImporter.SaveSpriteLibrarySourceAsset(savedAsset, path);
            }
        }
        
        void DoMainAssetGUI()
        {
            EditorGUI.BeginChangeCheck();
            if (m_PrimaryLibraryGUID.hasMultipleDifferentValues)
                EditorGUI.showMixedValue = true;
            var currentMainSpriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(AssetDatabase.GUIDToAssetPath(m_PrimaryLibraryGUID.stringValue));
            var newMainLibraryAsset = EditorGUILayout.ObjectField(Style.mainAssetLabel, currentMainSpriteLibraryAsset, typeof(SpriteLibraryAsset), false) as SpriteLibraryAsset;
            if (EditorGUI.EndChangeCheck())
            {
                var successfulAssignment = true;
                for (var i = 0; i < targets.Length; ++i)
                    successfulAssignment = AssignNewMainLibrary(targets[i], extraDataTargets[i] as SpriteLibrarySourceAsset, newMainLibraryAsset);

                if (successfulAssignment)
                    m_PrimaryLibraryGUID.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newMainLibraryAsset));
            }

            EditorGUI.showMixedValue = false;
        }
        
        static bool AssignNewMainLibrary(Object target, SpriteLibrarySourceAsset extraTarget, SpriteLibraryAsset newMainLibrary)
        {
            var assetPath = ((AssetImporter)target).assetPath;
            var spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(assetPath);
            var parentChain = SpriteLibrarySourceAssetImporter.GetAssetParentChain(newMainLibrary);
            if (assetPath == AssetDatabase.GetAssetPath(newMainLibrary) || parentChain.Contains(spriteLibraryAsset))
            {
                Debug.LogWarning(TextContent.spriteLibraryCircularDependency);
                return false;
            }          

            var path = ((AssetImporter)target).assetPath;
            var toSavedAsset = SpriteLibrarySourceAssetImporter.LoadSpriteLibrarySourceAsset(path);

            toSavedAsset.InitializeWithAsset(extraTarget);
            var savedLibrarySerializedObject = new SerializedObject(toSavedAsset);
            SpriteLibraryDataInspector.UpdateLibraryWithNewMainLibrary(newMainLibrary, savedLibrarySerializedObject.FindProperty(SpriteLibrarySourceAssetPropertyString.library));
            if (savedLibrarySerializedObject.hasModifiedProperties)
                savedLibrarySerializedObject.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }        
        
        void DoLibraryGUI()
        { 
            if (targets.Length == 1)
                m_SpriteLibraryDataInspector.OnGUI();
            else
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(Style.multiEditWarning, MessageType.Info);
            }
        }
    }
    
    internal class CreateSpriteLibrarySourceAsset : ProjectWindowCallback.EndNameEditAction
    {
        const int k_SpriteLibraryAssetMenuPriority = 30;
        
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var asset = CreateInstance<SpriteLibrarySourceAsset>();
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { asset }, pathName, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        
        [MenuItem("Assets/Create/2D/Sprite Library Asset", priority = k_SpriteLibraryAssetMenuPriority)]
        static void CreateSpriteLibrarySourceAssetMenu()
        {
            var action = CreateInstance<CreateSpriteLibrarySourceAsset>();
            var icon = IconUtility.LoadIconResource("Sprite Library", "Icons/Light", "Icons/Dark");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, SpriteLibrarySourceAsset.defaultName + SpriteLibrarySourceAsset.extension, icon, null);
        }
    }
}
