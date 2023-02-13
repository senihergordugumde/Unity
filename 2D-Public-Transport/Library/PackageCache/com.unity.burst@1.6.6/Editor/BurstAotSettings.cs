#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;

namespace Unity.Burst.Editor
{
    internal enum AvailX86Targets
    {
        SSE2 = (int)BurstTargetCpu.X86_SSE2,
        SSE4 = (int)BurstTargetCpu.X86_SSE4,
    }

    [Flags]
    internal enum BitsetX86Targets
    {
        SSE2 = 1 << AvailX86Targets.SSE2,
        SSE4 = 1 << AvailX86Targets.SSE4,
    }

    internal enum AvailX64Targets
    {
        SSE2 = (int)BurstTargetCpu.X64_SSE2,
        SSE4 = (int)BurstTargetCpu.X64_SSE4,
        AVX = (int)BurstTargetCpu.AVX,
        AVX2 = (int)BurstTargetCpu.AVX2,
    }

    [Flags]
    internal enum BitsetX64Targets
    {
        SSE2 = 1 << AvailX64Targets.SSE2,
        SSE4 = 1 << AvailX64Targets.SSE4,
        AVX = 1 << AvailX64Targets.AVX,
        AVX2 = 1 << AvailX64Targets.AVX2,
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class BurstMetadataSettingAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    internal class BurstCommonSettingAttribute : Attribute {}

    class BurstPlatformLegacySettings : ScriptableObject
    {
        [SerializeField]
        internal bool DisableOptimisations;
        [SerializeField]
        internal bool DisableSafetyChecks;
        [SerializeField]
        internal bool DisableBurstCompilation;

        BurstPlatformLegacySettings(BuildTarget target)
        {
            DisableSafetyChecks = true;
            DisableBurstCompilation = false;
            DisableOptimisations = false;
        }
    }

    // To add a setting,
    //  Add a
    //          [SerializeField] internal type settingname;
    //  Add a
    //          internal static readonly string settingname_DisplayName = "Name of option to be displayed in the editor (and searched for)";
    //  Add a
    //          internal static readonly string settingname_ToolTip = "tool tip information to display when hovering mouse
    // If the setting should be restricted to e.g. Standalone platform :
    //
    //  Add a
    //          internal static bool settingname_Display(BuildTarget selectedTarget) {}
    class BurstPlatformAotSettings : ScriptableObject
    {
        [SerializeField]
        [BurstMetadataSetting] // We always need version to be in our saved settings!
        internal int Version;
        [SerializeField]
        internal bool EnableBurstCompilation;
        [SerializeField]
        internal bool EnableOptimisations;
        [SerializeField]
        internal bool EnableSafetyChecks;
        [SerializeField]
        internal bool EnableDebugInAllBuilds;
        [SerializeField]
        internal bool UsePlatformSDKLinker;
        [SerializeField]
        internal AvailX86Targets CpuMinTargetX32;
        [SerializeField]
        internal AvailX86Targets CpuMaxTargetX32;
        [SerializeField]
        internal AvailX64Targets CpuMinTargetX64;
        [SerializeField]
        internal AvailX64Targets CpuMaxTargetX64;
        [SerializeField]
        internal BitsetX86Targets CpuTargetsX32;
        [SerializeField]
        internal BitsetX64Targets CpuTargetsX64;
        [SerializeField]
        internal OptimizeFor OptimizeFor;
        [SerializeField]
        [BurstCommonSetting]
        internal string DisabledWarnings;

        internal static readonly string EnableDebugInAllBuilds_DisplayName = "Force Debug Information";
        internal static readonly string EnableDebugInAllBuilds_ToolTip = "Generates debug information for the Burst-compiled code, irrespective of if Development Mode is ticked. This can be used to generate symbols for release builds for platforms that need it.";

        internal static readonly string EnableOptimisations_DisplayName = "Enable Optimisations";
        internal static readonly string EnableOptimisations_ToolTip = "Enables all optimisations for the currently selected platform.";

        internal static readonly string EnableBurstCompilation_DisplayName = "Enable Burst Compilation";
        internal static readonly string EnableBurstCompilation_ToolTip = "Enables burst compilation for the selected platform.";

        internal static readonly string OptimizeFor_DisplayName = "Optimize For";
        internal static readonly string OptimizeFor_ToolTip = "Choose what optimization setting to compile Burst code for.";

        internal static readonly string DisabledWarnings_DisplayName = "Disabled Warnings*";
        internal static readonly string DisabledWarnings_ToolTip = "Burst warnings to disable (separated by ;).";

        internal static readonly string UsePlatformSDKLinker_DisplayName = "Use Platform SDK Linker";
        internal static readonly string UsePlatformSDKLinker_ToolTip = "Enabling this option will disable cross compilation support for desktops, and will require platform specific tools for Windows/Linux/Mac - use only if you encounter problems with the burst builtin solution.";

        internal static bool UsePlatformSDKLinker_Display(BuildTarget selectedTarget)
        {
            return IsStandalone(selectedTarget);
        }

        internal static readonly string CpuTargetsX32_DisplayName = "Target 32Bit CPU Architectures";
        internal static readonly string CpuTargetsX32_ToolTip = "Use this to specify the set of target architectures to support for the currently selected platform.";
        internal static bool CpuTargetsX32_Display(BuildTarget selectedTarget)
        {
            return (IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer) && Has32BitSupport(selectedTarget);
        }

        internal static readonly string CpuTargetsX64_DisplayName = "Target 64Bit CPU Architectures";
        internal static readonly string CpuTargetsX64_ToolTip = "Use this to specify the target architectures to support for the currently selected platform.";
        internal static bool CpuTargetsX64_Display(BuildTarget selectedTarget)
        {
            return IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer;
        }

        internal static bool IsStandalone(BuildTarget target)
        {
            switch (target)
            {
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux:
#endif
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                    return true;
                default:
                    return false;
            }
        }

        BurstPlatformAotSettings(BuildTarget target)
        {
            InitialiseDefaults();
        }

        private const int DefaultVersion = 4;

        internal void InitialiseDefaults()
        {
            Version = DefaultVersion;
            EnableSafetyChecks = false;
            EnableBurstCompilation = true;
            EnableOptimisations = true;
            EnableDebugInAllBuilds = false;
            UsePlatformSDKLinker = false; // Only applicable for desktop targets (Windows/Mac/Linux)
            CpuMinTargetX32 = 0;
            CpuMaxTargetX32 = 0;
            CpuMinTargetX64 = 0;
            CpuMaxTargetX64 = 0;
            CpuTargetsX32 = BitsetX86Targets.SSE2 | BitsetX86Targets.SSE4;
            CpuTargetsX64 = BitsetX64Targets.SSE2 | BitsetX64Targets.AVX2;
            DisabledWarnings = "";
            OptimizeFor = OptimizeFor.Default;
        }

        internal static string GetPath(BuildTarget? target)
        {
            if (target.HasValue)
            {
                return "ProjectSettings/BurstAotSettings_" + target.ToString() + ".json";
            }
            else
            {
                return "ProjectSettings/CommonBurstAotSettings.json";
            }
        }

        internal static BuildTarget? ResolveTarget(BuildTarget? target)
        {
            if (!target.HasValue)
            {
                return target;
            }

            // Treat the 32/64 platforms the same from the point of view of burst settings
            // since there is no real way to distinguish from the platforms selector
            if (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows)
                return BuildTarget.StandaloneWindows;

#if UNITY_2019_2_OR_NEWER
            // 32 bit linux support was deprecated
            if (target == BuildTarget.StandaloneLinux64)
                return BuildTarget.StandaloneLinux64;
#else
            if (target == BuildTarget.StandaloneLinux64 || target == BuildTarget.StandaloneLinux)
                return BuildTarget.StandaloneLinux64;
#endif

            return target;
        }

        internal static readonly string BurstMiscPathPostFix = "_BurstDebugInformation_DoNotShip";
        internal static string FetchOutputPath(BuildReport report)
        {
            var finalOutputPath = report.summary.outputPath;
            var directoryName = Path.GetDirectoryName(finalOutputPath);
            var appName = Path.GetFileNameWithoutExtension(finalOutputPath);

            return $"{Path.Combine(directoryName, appName)}{BurstMiscPathPostFix}";
        }

        internal static BurstPlatformAotSettings GetOrCreateSettings(BuildTarget? target)
        {
            target = ResolveTarget(target);
            var settings = CreateInstance<BurstPlatformAotSettings>();
            settings.InitialiseDefaults();
            string path = GetPath(target);

            var fileExists = File.Exists(path);
            var upgraded = false;
            if (fileExists)
            {
                var json = File.ReadAllText(path);
                settings = SerialiseIn(target, json, out upgraded);
            }

            if (!fileExists || upgraded)
            {
                // If the settings file didn't previously exist,
                // or it did exist but we've just upgraded it to a new version,
                // save it to disk now.
                settings.Save(target);
            }

            // Overwrite the settings with any that are common and shared between all settings.
            if (target.HasValue)
            {
                var commonSettings = GetOrCreateSettings(null);

                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in platformFields)
                {
                    if (null != field.GetCustomAttribute<BurstCommonSettingAttribute>())
                    {
                        field.SetValue(settings, field.GetValue(commonSettings));
                    }
                }
            }

            return settings;
        }

        delegate bool SerialiseItem(BuildTarget selectedPlatform);

        private static BurstPlatformAotSettings SerialiseIn(BuildTarget? target, string json, out bool upgraded)
        {
            var versioned = ScriptableObject.CreateInstance<BurstPlatformAotSettings>();
            EditorJsonUtility.FromJsonOverwrite(json, versioned);

            upgraded = false;

            if (versioned.Version == 0)
            {
                // Deal with pre versioned format
                var legacy = ScriptableObject.CreateInstance<BurstPlatformLegacySettings>();
                EditorJsonUtility.FromJsonOverwrite(json, legacy);

                // Legacy file, upgrade it
                versioned.InitialiseDefaults();
                versioned.EnableOptimisations = !legacy.DisableOptimisations;
                versioned.EnableBurstCompilation = !legacy.DisableBurstCompilation;
                versioned.EnableSafetyChecks = !legacy.DisableSafetyChecks;

                // Destroy the legacy object so Unity doesn't try to backup / restore it later during domain reload.
                ScriptableObject.DestroyImmediate(legacy);

                upgraded = true;
            }

            if (versioned.Version < 3)
            {
                // Upgrade the version first
                versioned.Version = 3;

                // Upgrade from min..max targets to bitset
                versioned.CpuTargetsX32 |= (BitsetX86Targets)(1 << (int)versioned.CpuMinTargetX32);
                versioned.CpuTargetsX32 |= (BitsetX86Targets)(1 << (int)versioned.CpuMaxTargetX32);

                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)versioned.CpuMinTargetX64);
                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)versioned.CpuMaxTargetX64);

                // Extra checks to add targets in the min..max range for 64-bit targets.
                switch (versioned.CpuMinTargetX64)
                {
                    default:
                        break;
                    case AvailX64Targets.SSE2:
                        switch (versioned.CpuMaxTargetX64)
                        {
                            default:
                                break;
                            case AvailX64Targets.AVX2:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.AVX);
                                goto case AvailX64Targets.AVX;
                            case AvailX64Targets.AVX:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.SSE4);
                                break;
                        }
                        break;
                    case AvailX64Targets.SSE4:
                        switch (versioned.CpuMaxTargetX64)
                        {
                            default:
                                break;
                            case AvailX64Targets.AVX2:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.AVX);
                                break;
                        }
                        break;
                }

                // Wipe the old min/max targets
                versioned.CpuMinTargetX32 = 0;
                versioned.CpuMaxTargetX32 = 0;
                versioned.CpuMinTargetX64 = 0;
                versioned.CpuMaxTargetX64 = 0;

                upgraded = true;
            }

            if (versioned.Version < 4)
            {
                // Upgrade the version first.
                versioned.Version = 4;

                // When we upgrade we'll set the optimization level to default (which is, as expected, the default).
                versioned.OptimizeFor = OptimizeFor.Default;

                // This option has been removed as user-setting options, so switch them to false here.
                versioned.EnableSafetyChecks = false;

                upgraded = true;
            }

            // Otherwise should be a modern file with a valid version (we can use that to upgrade when the time comes)
            return versioned;
        }

        private static bool ShouldSerialiseOut(BuildTarget? target, FieldInfo field)
        {
            var method = typeof(BurstPlatformAotSettings).GetMethod(field.Name + "_Display", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                var shouldSerialise = (SerialiseItem)Delegate.CreateDelegate(typeof(SerialiseItem), method);
                if (!target.HasValue || !shouldSerialise(target.Value))
                {
                    return false;
                }
            }

            // If we always need to write out the attribute, return now.
            if (null != field.GetCustomAttribute<BurstMetadataSettingAttribute>())
            {
                return true;
            }

            var isCommon = !target.HasValue;
            var hasCommonAttribute = null != field.GetCustomAttribute<BurstCommonSettingAttribute>();

            if ((isCommon && hasCommonAttribute) || (!isCommon && !hasCommonAttribute))
            {
                return true;
            }

            return false;
        }

        internal string SerialiseOut(BuildTarget? target)
        {
            // Version 2 and onwards serialise a custom object in order to avoid serialising all the settings.
            StringBuilder s = new StringBuilder();
            s.Append("{\n");
            s.Append("  \"MonoBehaviour\": {\n");
            var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            int total = 0;
            for (int i = 0; i < platformFields.Length; i++)
            {
                if (ShouldSerialiseOut(target, platformFields[i]))
                {
                    total++;
                }
            }
            for (int i = 0; i < platformFields.Length; i++)
            {
                if (ShouldSerialiseOut(target, platformFields[i]))
                {
                    s.Append($"    \"{platformFields[i].Name}\": ");
                    if (platformFields[i].FieldType.IsEnum)
                        s.Append((int)platformFields[i].GetValue(this));
                    else if (platformFields[i].FieldType == typeof(string))
                        s.Append($"\"{platformFields[i].GetValue(this)}\"");
                    else if (platformFields[i].FieldType == typeof(bool))
                        s.Append(((bool)platformFields[i].GetValue(this)) ? "true" : "false");
                    else
                        s.Append((int)platformFields[i].GetValue(this));

                    total--;
                    if (total != 0)
                        s.Append(",");
                    s.Append("\n");
                }
            }
            s.Append("  }\n");
            s.Append("}\n");

            return s.ToString();
        }

        internal void Save(BuildTarget? target)
        {
            if (target.HasValue)
            {
                target = ResolveTarget(target);
            }

            var path = GetPath(target);

#if UNITY_2019_3_OR_NEWER
            if (!AssetDatabase.IsOpenForEdit(path))
            {
                if (!AssetDatabase.MakeEditable(path))
                {
                    Debug.LogWarning($"Burst could not save AOT settings file {path}");
                    return;
                }
            }
#endif

            File.WriteAllText(path, SerialiseOut(target));
        }

        internal static SerializedObject GetCommonSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings(null));
        }

        internal static SerializedObject GetSerializedSettings(BuildTarget target)
        {
            return new SerializedObject(GetOrCreateSettings(target));
        }

        internal static bool Has32BitSupport(BuildTarget target)
        {
            switch (target)
            {
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
#endif
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:
                    return true;
                default:
                    return false;
            }
        }

        private static BurstTargetCpu GetCpu(int v)
        {
            // https://graphics.stanford.edu/~seander/bithacks.html#IntegerLog
            var r = ((v > 0xFFFF) ? 1 : 0) << 4; v >>= r;
            var shift = ((v > 0xFF) ? 1 : 0) << 3; v >>= shift; r |= shift;
            shift = ((v > 0xF) ? 1 : 0) << 2; v >>= shift; r |= shift;
            shift = ((v > 0x3) ? 1 : 0) << 1; v >>= shift; r |= shift;
            r |= (v >> 1);
            return (BurstTargetCpu)r;
        }

        private static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        internal TargetCpus GetDesktopCpu32Bit()
        {
            var cpus = new TargetCpus();

            foreach (var target in GetFlags(CpuTargetsX32))
            {
                cpus.Cpus.Add(GetCpu((int)(BitsetX86Targets)target));
            }

            // If no targets were specified just default to the oldest CPU supported.
            if (cpus.Cpus.Count == 0)
            {
                cpus.Cpus.Add(BurstTargetCpu.X86_SSE2);
            }

            return cpus;
        }

        internal TargetCpus GetDesktopCpu64Bit()
        {
            var cpus = new TargetCpus();

            foreach (var target in GetFlags(CpuTargetsX64))
            {
                cpus.Cpus.Add(GetCpu((int)(BitsetX64Targets)target));
            }

            // If no targets were specified just default to the oldest CPU supported.
            if (cpus.Cpus.Count == 0)
            {
                cpus.Cpus.Add(BurstTargetCpu.X64_SSE2);
            }

            return cpus;
        }
    }

    static class BurstAotSettingsIMGUIRegister
    {
        class BurstAotSettingsProvider : SettingsProvider
        {
            SerializedObject[] m_PlatformSettings;
            SerializedProperty[][] m_PlatformProperties;
            DisplayItem[][] m_PlatformVisibility;
            GUIContent[][] m_PlatformToolTips;
            BuildPlatform[] m_ValidPlatforms;
            SerializedObject m_CommonPlatformSettings;

            delegate bool DisplayItem(BuildTarget selectedTarget);

            static bool DefaultShow(BuildTarget selectedTarget)
            {
                return true;
            }
            static bool DefaultHide(BuildTarget selectedTarget)
            {
                return false;
            }

            public BurstAotSettingsProvider()
                : base("Project/Burst AOT Settings", SettingsScope.Project, null)
            {
                int a;

                m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms(true).ToArray();

                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                int numPlatformFields = platformFields.Length;
                int numKeywords = numPlatformFields;
                var tempKeywords = new string[numKeywords];

                for (a = 0; a < numPlatformFields; a++)
                {
                    tempKeywords[a] = typeof(BurstPlatformAotSettings).GetField(platformFields[a].Name + "_ToolTip", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                }

                keywords = new HashSet<string>(tempKeywords);

                m_PlatformSettings = new SerializedObject[m_ValidPlatforms.Length];
                m_PlatformProperties = new SerializedProperty[m_ValidPlatforms.Length][];
                m_PlatformVisibility = new DisplayItem[m_ValidPlatforms.Length][];
                m_PlatformToolTips = new GUIContent[m_ValidPlatforms.Length][];

                m_CommonPlatformSettings = null;
            }

            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                for (int p = 0; p < m_ValidPlatforms.Length; p++)
                {
                    InitialiseSettingsForCommon(platformFields);
                    InitialiseSettingsForPlatform(p, platformFields);
                }
            }

            private void InitialiseSettingsForCommon(FieldInfo[] commonFields)
            {
                m_CommonPlatformSettings = BurstPlatformAotSettings.GetCommonSerializedSettings();
            }

            private void InitialiseSettingsForPlatform(int platform, FieldInfo[] platformFields)
            {
                if (m_ValidPlatforms[platform].targetGroup == BuildTargetGroup.Standalone)
                    m_PlatformSettings[platform] = BurstPlatformAotSettings.GetSerializedSettings(EditorUserBuildSettings.selectedStandaloneTarget);
                else
                    m_PlatformSettings[platform] = BurstPlatformAotSettings.GetSerializedSettings(m_ValidPlatforms[platform].defaultTarget);

                m_PlatformProperties[platform] = new SerializedProperty[platformFields.Length];
                m_PlatformToolTips[platform] = new GUIContent[platformFields.Length];
                m_PlatformVisibility[platform] = new DisplayItem[platformFields.Length];
                for (int i = 0; i < platformFields.Length; i++)
                {
                    m_PlatformProperties[platform][i] = m_PlatformSettings[platform].FindProperty(platformFields[i].Name);
                    var displayName = typeof(BurstPlatformAotSettings).GetField(platformFields[i].Name + "_DisplayName", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                    var toolTip = typeof(BurstPlatformAotSettings).GetField(platformFields[i].Name + "_ToolTip", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                    m_PlatformToolTips[platform][i] = EditorGUIUtility.TrTextContent(displayName, toolTip);

                    var method = typeof(BurstPlatformAotSettings).GetMethod(platformFields[i].Name + "_Display", BindingFlags.Static | BindingFlags.NonPublic);
                    if (method == null)
                    {
                        if (displayName == null)
                        {
                            m_PlatformVisibility[platform][i] = DefaultHide;
                        }
                        else
                        {
                            m_PlatformVisibility[platform][i] = DefaultShow;
                        }
                    }
                    else
                    {
                        m_PlatformVisibility[platform][i] = (DisplayItem)Delegate.CreateDelegate(typeof(DisplayItem), method);
                    }
                }
            }

            private string FetchStandaloneTargetName()
            {
                switch (EditorUserBuildSettings.selectedStandaloneTarget)
                {
                    case BuildTarget.StandaloneOSX:
                        return "Mac OS X";    // Matches the Build Settings Dialog names
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        return "Windows";
                    default:
                        return "Linux";
                }
            }

            public override void OnGUI(string searchContext)
            {
                var rect = EditorGUILayout.BeginVertical();

                EditorGUIUtility.labelWidth = rect.width / 2;

                int selectedPlatform = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms, null);

                // During a build and other cases, the settings object can become invalid, if it does, we re-build it for the current platform
                // this fixes the settings failing to save if modified after a build has finished, and the settings were still open
                if (!m_PlatformSettings[selectedPlatform].isValid)
                {
                    var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    InitialiseSettingsForCommon(platformFields);

                    // If the selected platform is invalid, it means all of them will be. So we do a pass to reinitialize all now.
                    for (var platform = 0; platform < m_PlatformSettings.Length; platform++)
                    {
                        InitialiseSettingsForPlatform(platform, platformFields);
                    }
                }

                var selectedTarget = m_ValidPlatforms[selectedPlatform].defaultTarget;
                if (m_ValidPlatforms[selectedPlatform].targetGroup == BuildTargetGroup.Standalone)
                    selectedTarget = EditorUserBuildSettings.selectedStandaloneTarget;

                if (m_ValidPlatforms[selectedPlatform].targetGroup == BuildTargetGroup.Standalone)
                {
                    // Note burst treats Windows and Windows32 as the same target from a settings point of view (same for linux)
                    // So we only display the standalone platform
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Target Platform", "Shows the currently selected standalone build target, can be switched in the Build Settings dialog"), EditorGUIUtility.TrTextContent(FetchStandaloneTargetName()));
                }

                for (int i = 0; i < m_PlatformProperties[selectedPlatform].Length; i++)
                {
                    if (m_PlatformVisibility[selectedPlatform][i](selectedTarget))
                    {
                        EditorGUILayout.PropertyField(m_PlatformProperties[selectedPlatform][i], m_PlatformToolTips[selectedPlatform][i]);
                    }
                }

                EditorGUILayout.EndPlatformGrouping();

                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField("* Shared setting common across all platforms");

                if (m_PlatformSettings[selectedPlatform].hasModifiedProperties)
                {
                    m_PlatformSettings[selectedPlatform].ApplyModifiedPropertiesWithoutUndo();

                    var commonAotSettings = ((BurstPlatformAotSettings)m_CommonPlatformSettings.targetObject);
                    var platformAotSettings = ((BurstPlatformAotSettings)m_PlatformSettings[selectedPlatform].targetObject);

                    var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var field in platformFields)
                    {
                        if (null != field.GetCustomAttribute<BurstCommonSettingAttribute>())
                        {
                            field.SetValue(commonAotSettings, field.GetValue(platformAotSettings));

                            foreach (var platformSetting in m_PlatformSettings)
                            {
                                field.SetValue(platformSetting.targetObject, field.GetValue(commonAotSettings));
                            }
                        }
                    }

                    commonAotSettings.Save(null);
                    platformAotSettings.Save(selectedTarget);
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateBurstAotSettingsProvider()
        {
            return new BurstAotSettingsProvider();
        }
    }
}
#else
// Mirror old behaviour
namespace Unity.Burst.Editor
{
    class BurstPlatformAotSettings
    {
        internal bool EnableOptimisations;
        internal bool EnableSafetyChecks;
        internal bool EnableBurstCompilation;
        internal bool EnableDebugInAllBuilds;
        internal bool UsePlatformSDKLinker;
        internal string DisabledWarnings;

        internal static BurstPlatformAotSettings GetOrCreateSettings(BuildTarget target)
        {
            BurstPlatformAotSettings settings = new BurstPlatformAotSettings();

            settings.EnableOptimisations = true;
            settings.EnableSafetyChecks = BurstEditorOptions.EnableBurstSafetyChecks;
            settings.EnableBurstCompilation = BurstEditorOptions.EnableBurstCompilation;
            settings.UsePlatformSDKLinker = false;
            settings.EnableDebugInAllBuilds = false;
            settings.DisabledWarnings = "";

            return settings;
        }

        internal TargetCpus GetDesktopCpu32Bit()
        {
            var cpus = new TargetCpus();

            cpus.Cpus.Add(BurstTargetCpu.X86_SSE2);
            cpus.Cpus.Add(BurstTargetCpu.X86_SSE4);

            return cpus;
        }

        internal TargetCpus GetDesktopCpu64Bit()
        {
            var cpus = new TargetCpus();

            cpus.Cpus.Add(BurstTargetCpu.X64_SSE2);
            cpus.Cpus.Add(BurstTargetCpu.X64_SSE4);

            return cpus;
        }
    }
}

#endif
#endif