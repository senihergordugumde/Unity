using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal static class TextContent
    {
        // Undo
        public static string setMode = "Set Mode";
        public static string setTool = "Set Tool";
        public static string pasteData = "Paste Data";
        public static string generateGeometry = "Generate Geometry";
        public static string generateWeights = "Generate Weights";
        public static string normalizeWeights = "Normalize Weights";
        public static string clearWeights = "Clear Weights";
        public static string restorePose = "Restore Pose";
        public static string selection = "Selection";
        public static string clearSelection = "Clear Selection";
        public static string editWeights = "Edit Weights";
        public static string boneName = "Bone Name";
        public static string boneDepth = "Bone Depth";
        public static string boneColor = "Bone Color";
        public static string rotateBone = "Rotate Bone";
        public static string moveBone = "Move Bone";
        public static string colorBoneChanged = "Bone Color";
        public static string freeMoveBone = "Free Move Bone";
        public static string moveJoint = "Move Joint";
        public static string moveEndPoint = "Move End Point";
        public static string boneLength = "Bone Length";
        public static string createBone = "Create Bone";
        public static string splitBone = "Split Bone";
        public static string removeBone = "Remove Bone";
        public static string moveVertices = "Move Vertices";
        public static string createVertex = "Create Vertex";
        public static string createEdge = "Create Edge";
        public static string splitEdge = "Split Edge";
        public static string removeEdge = "Remove Edge";
        public static string removeVertices = "Remove Vertices";
        public static string selectionChange = "Selection Change";
        public static string boneVisibility = "Bone Visibility";
        public static string setParentBone = "Set Parent Bone";
        public static string visibilityChange = "VisibilityChange";
        public static string boneSelection = "Bone Selection";
        public static string expandBones = "Expand Bones";
        public static string meshVisibility = "Mesh Visibility";
        public static string meshOpacity = "Mesh Opacity";
        public static string opacityChange = "Opacity Change";
        public static string spriteVisibility = "SpriteVisibility";
        public static string visibilityTab = "Visibility Tab";
        public static string addBoneInfluence = "Add Bone Influence";
        public static string removeBoneInfluence = "Remove Bone Influence";
        public static string reorderBoneInfluence = "Reorder Bone Influence";
        public static string addSpriteInfluence = "Add Sprite Influence";
        public static string removeSpriteInfluence = "Remove Sprite Influence";
        public static string spriteSelection = "Sprite selection";

        // Tooltips
        public static string visibilityIconTooltip = L10n.Tr("Visibility tool");
        public static string characterIconTooltip = L10n.Tr("Restore bind pose");
        public static string spriteSheetIconTooltip = L10n.Tr("Switch between Sprite sheet and Character mode");
        public static string copyTooltip = L10n.Tr("Copy");
        public static string pasteTooltip = L10n.Tr("Paste");
        public static string onTooltip = L10n.Tr("On");
        public static string offTooltip = L10n.Tr("Off");
        public static string addBoneInfluenceTooltip = L10n.Tr("Add selected bone to influence Sprite");
        public static string removeBoneInfluenceTooltip = L10n.Tr("Remove selected bone from influencing Sprite");
        public static string addSpriteInfluenceTooltip = L10n.Tr("Add selected Sprite to be influenced by bone");
        public static string removeSpriteInfluenceTooltip = L10n.Tr("Remove selected Sprite from being influenced by bone");

        // Horizontal tool bar button txt
        public static string visibilityIconText = L10n.Tr("Visibility");
        public static string spriteSheetIconText = L10n.Tr("Sprite Sheet");
        public static string copyText = L10n.Tr("Copy");
        public static string pasteText = L10n.Tr("Paste");

        // Settings
        public static string selectedOutlineColor = L10n.Tr("Selected Outline Color");
        public static string spriteOutlineSize = L10n.Tr("Sprite Outline Size");
        public static string boneOutlineSize = L10n.Tr("Bone Outline Size");

        // Sprite Library
        public static string convertGroupToCategory = L10n.Tr("Convert Group to Category");
        public static string newTrailingDots = L10n.Tr("New...");
        public static string removeEmptyCategory = L10n.Tr("Remove Empty Category");
        public static string convertLayerToCategory = L10n.Tr("Convert Layer to Category");
        public static string clearAllCategory = L10n.Tr("Clear All Category");
        public static string spriteCategoryChanged = L10n.Tr("Sprite Category Changed");
        public static string spriteCategoryIndexChanged = L10n.Tr("Sprite Category Index Changed");
        public static string categoryList = L10n.Tr("Category List");
        public static string label = L10n.Tr("Label");
        public static string spriteLibraryCircularDependency = L10n.Tr("The Sprite Library can't be added because self-referencing is not allowed. Try another Sprite Library instead.");

        // Other
        public static string generatingGeometry = L10n.Tr("Generating Geometry");
        public static string generatingWeights = L10n.Tr("Generating Weights");
        public static string restorePoseLocalized = L10n.Tr("Restore Pose");
        public static string vertexWeight = L10n.Tr("Vertex Weight");
        public static string vertexWeightToolTip = L10n.Tr("Adjust bone weights for selected vertex");
        public static string bone = L10n.Tr("Bone");
        public static string depth = L10n.Tr("Depth");
        public static string color = L10n.Tr("Color");
        public static string sprite = L10n.Tr("Sprite");
        public static string name = L10n.Tr("Name");
        public static string none = L10n.Tr("None");
        public static string size = L10n.Tr("Size");
        public static string noSpriteSelected = L10n.Tr("No sprite selected");
        public static string weightSlider = L10n.Tr("Weight Slider");
        public static string weightBrush = L10n.Tr("Weight Brush");
        public static string generateAll = L10n.Tr("Generate All");
        public static string generate = L10n.Tr("Generate");
        public static string mode = L10n.Tr("Mode");
        public static string modeTooltip = L10n.Tr("Different operation mode for weight adjustment");
        public static string boneToolTip = L10n.Tr("The bone that is affecting");
        
        public static string noBoneSelected = L10n.Tr("No bone selected");
        public static string boneInfluences = L10n.Tr("Bone Influences");
        public static string influencedSprites = L10n.Tr("Sprite Influences");

        // Error messages
        public static string smoothMeshError = "Generated mesh could not be smoothed. Please try using different parameters";
        public static string copyError1 = "Unable to convert copied data to Skinning Module paste data.";
        public static string copyError2 = "There is no copied data to paste.";
        public static string copyError3 = "Target has a different number of Sprites ({0}) compared to copied source ({1}) for Mesh copy.";
    }
}
