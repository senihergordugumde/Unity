#if UNITY_EDITOR && ENABLE_BURST_AOT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Scripting.Compilers;
using UnityEditor.UnityLinker;
using UnityEditor.Utils;
using UnityEngine;
using CompilerMessageType = UnityEditor.Scripting.Compilers.CompilerMessageType;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR_OSX
using System.ComponentModel;
using Unity.Burst.LowLevel;
using UnityEditor.Callbacks;
#endif

namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    internal class TargetCpus
    {
        public List<BurstTargetCpu> Cpus;

        public TargetCpus()
        {
            Cpus = new List<BurstTargetCpu>();
        }

        public TargetCpus(BurstTargetCpu single)
        {
            Cpus = new List<BurstTargetCpu>(1)
            {
                single
            };
        }

        public bool IsX86()
        {
            foreach (var cpu in Cpus)
            {
                switch (cpu)
                {
                    case BurstTargetCpu.X86_SSE2:
                    case BurstTargetCpu.X86_SSE4:
                        return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            var result = "";

            var first = true;

            foreach (var cpu in Cpus)
            {
                if (first)
                {
                    result += $"{cpu}";
                    first = false;
                }
                else
                {
                    result += $", {cpu}";
                }
            }

            return result;
        }

        public TargetCpus Clone()
        {
            var copy = new TargetCpus
            {
                Cpus = new List<BurstTargetCpu>(Cpus.Count)
            };

            foreach (var cpu in Cpus)
            {
                copy.Cpus.Add(cpu);
            }

            return copy;
        }
    }

    internal class LinkXMLGenerator : IUnityLinkerProcessor
    {
        public int callbackOrder => 1;
        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var linkXml = Path.GetFullPath(Path.Combine("Temp", BurstAotCompiler.BurstLinkXmlName));

            return linkXml;
        }

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
    }

    internal class BurstAndroidGradlePostprocessor : IPostGenerateGradleAndroidProject
    {
        int IOrderedCallback.callbackOrder => 1;

        void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path)
        {
            var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(BuildTarget.Android);
            // Early exit if burst is not activated
            if (!aotSettingsForTarget.EnableBurstCompilation)
            {
                return;
            }

            // Copy bursted .so's from tempburstlibs to the actual location in the gradle project
            var sourceLocation = Path.GetFullPath(Path.Combine("Temp", "StagingArea", "tempburstlibs"));
            var targetLocation = Path.GetFullPath(Path.Combine(path, "src", "main", "jniLibs"));
            FileUtil.CopyDirectoryRecursive(sourceLocation, targetLocation, true);
        }
    }

    // For static builds, there are two different approaches:
    // Postprocessing adds the libraries after Unity is done building,
    // for platforms that need to build a project file, etc.
    // Preprocessing simply adds the libraries to the Unity build,
    // for platforms where Unity can directly build an app.
    internal class StaticPreProcessor : IPreprocessBuildWithReport
    {
        private const string TempSourceLibrary = @"Temp/StagingArea/SourcePlugins";
        private const string TempStaticLibrary = @"Temp/StagingArea/NativePlugins";
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(report.summary.platform);

            // Early exit if burst is not activated
            if (!aotSettingsForTarget.EnableBurstCompilation)
            {
                return;
            }

            if(report.summary.platform == BuildTarget.Switch)
            {
                // add the static lib, and the c++ shim
                string burstCppLinkFile = "lib_burst_generated.cpp";
                string burstStaticLibFile = "lib_burst_generated.a";
                string cppPath = Path.Combine(TempSourceLibrary, burstCppLinkFile);
                string libPath = Path.Combine(TempStaticLibrary, burstStaticLibFile);
                if(!Directory.Exists(TempSourceLibrary))
                {
                    Directory.CreateDirectory(TempSourceLibrary);
                    Directory.CreateDirectory(TempSourceLibrary);
                }
                File.WriteAllText(cppPath, @"
extern ""C""
{
    void Staticburst_initialize(void* );
    void* StaticBurstStaticMethodLookup(void* );

    int burst_enable_static_linkage = 1;
    void burst_initialize(void* i) { Staticburst_initialize(i); }
    void* BurstStaticMethodLookup(void* i) { return StaticBurstStaticMethodLookup(i); }
}
");
            }
        }
    }
    /// <summary>
    /// Integration of the burst AOT compiler into the Unity build player pipeline
    /// </summary>
    internal class BurstAotCompiler : IPostBuildPlayerScriptDLLs
    {
        private const string BurstAotCompilerExecutable = "bcl.exe";
        private const string TempStaging = @"Temp/StagingArea/";
        private const string TempStagingManaged = TempStaging + @"Data/Managed/";
        private const string LibraryPlayerScriptAssemblies = "Library/PlayerScriptAssemblies";
        private const string TempManagedSymbols = @"Temp/ManagedSymbols/";
        internal const string BurstLinkXmlName = "burst.link.xml";


        int IOrderedCallback.callbackOrder => 0;

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            var step = report.BeginBuildStep("burst");
            try
            {
                OnPostBuildPlayerScriptDLLsImpl(report);
            }
            finally
            {
                report.EndBuildStep(step);
            }
        }

        private void OnPostBuildPlayerScriptDLLsImpl(BuildReport report)
        {
            var buildTarget = report.summary.platform;

            string burstMiscAlongsidePath = "";
            if ((report.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                burstMiscAlongsidePath = BurstPlatformAotSettings.FetchOutputPath(report);
            }

            var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(buildTarget);
            HashSet<string> assemblyDefines = new HashSet<string>();

            // Early exit if burst is not activated or the platform is not supported
            if (BurstCompilerOptions.ForceDisableBurstCompilation || !aotSettingsForTarget.EnableBurstCompilation || !IsSupportedPlatform(buildTarget))
            {
                return;
            }

            var isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;

            var commonOptions = new List<string>();
            var stagingFolder = Path.GetFullPath(TempStagingManaged);

            var playerAssemblies = GetPlayerAssemblies(report);

            // grab the location of the root of the player folder - for handling nda platforms that require keys
            var keyFolder = BuildPipeline.GetPlaybackEngineDirectory(buildTarget, BuildOptions.None);
            commonOptions.Add(GetOption(OptionAotKeyFolder, keyFolder));
            commonOptions.Add(GetOption(OptionAotDecodeFolder, Path.Combine(Environment.CurrentDirectory, "Library", "Burst")));

            // Extract the TargetPlatform and Cpus from the current build settings
            var targetPlatform = GetTargetPlatformAndDefaultCpu(buildTarget, out var targetCpus);
            commonOptions.Add(GetOption(OptionPlatform, targetPlatform));

            // --------------------------------------------------------------------------------------------------------
            // 1) Calculate AssemblyFolders
            // These are the folders to look for assembly resolution
            // --------------------------------------------------------------------------------------------------------
            var assemblyFolders = new List<string> { stagingFolder };
            if (buildTarget == BuildTarget.WSAPlayer
                || buildTarget == BuildTarget.XboxOne
#if UNITY_2019_4_OR_NEWER
                || buildTarget == BuildTarget.GameCoreXboxOne || buildTarget == BuildTarget.GameCoreXboxSeries
#endif
                )
            {
                // On UWP, not all assemblies are copied to StagingArea, so we want to
                // find all directories that we can reference assemblies from
                // If we don't do this, we will crash with AssemblyResolutionException
                // when following type references.
                foreach (var assembly in playerAssemblies)
                {
                    foreach (var assemblyRef in assembly.compiledAssemblyReferences)
                    {
                        // Exclude folders with assemblies already compiled in the `folder`
                        var assemblyName = Path.GetFileName(assemblyRef);
                        if (assemblyName != null && File.Exists(Path.Combine(stagingFolder, assemblyName)))
                        {
                            continue;
                        }

                        var directory = Path.GetDirectoryName(assemblyRef);
                        if (directory != null)
                        {
                            var fullPath = Path.GetFullPath(directory);
                            if (IsMonoReferenceAssemblyDirectory(fullPath) || IsDotNetStandardAssemblyDirectory(fullPath))
                            {
                                // Don't pass reference assemblies to burst because they contain methods without implementation
                                // If burst accidentally resolves them, it will emit calls to burst_abort.
                                fullPath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono");
#if UNITY_2021_2_OR_NEWER
                                // In 2021.2 we got multiple mono distributions, per platform.
                                fullPath = Path.Combine(fullPath, "unityaot-" + BuildTargetDiscovery.GetPlatformProfileSuffix(buildTarget));
#else
                                fullPath = Path.Combine(fullPath, "unityaot");
#endif
                                fullPath = Path.GetFullPath(fullPath); // GetFullPath will normalize path separators to OS native format
                                if (!assemblyFolders.Contains(fullPath))
                                    assemblyFolders.Add(fullPath);

                                fullPath = Path.Combine(fullPath, "Facades");
                                if (!assemblyFolders.Contains(fullPath))
                                    assemblyFolders.Add(fullPath);
                            }
                            else if (!assemblyFolders.Contains(fullPath))
                            {
                                assemblyFolders.Add(fullPath);
                            }
                        }
                    }
                }
            }

            // Copy assembly used during staging to have a trace
            if (BurstLoader.IsDebugging)
            {
                try
                {
                    var copyAssemblyFolder = Path.Combine(Environment.CurrentDirectory, "Logs", "StagingAssemblies");
                    try
                    {
                        if (Directory.Exists(copyAssemblyFolder)) Directory.Delete(copyAssemblyFolder);
                    }
                    catch
                    {
                    }

                    if (!Directory.Exists(copyAssemblyFolder)) Directory.CreateDirectory(copyAssemblyFolder);
                    foreach (var file in Directory.EnumerateFiles(stagingFolder))
                    {
                        File.Copy(file, Path.Combine(copyAssemblyFolder, Path.GetFileName(file)));
                    }
                }
                catch
                {
                }
            }

            // --------------------------------------------------------------------------------------------------------
            // 2) Calculate root assemblies
            // These are the assemblies that the compiler will look for methods to compile
            // This list doesn't typically include .NET runtime assemblies but only assemblies compiled as part
            // of the current Unity project
            // --------------------------------------------------------------------------------------------------------
            var rootAssemblies = new List<string>();
            foreach (var playerAssembly in playerAssemblies)
            {
                // the file at path `playerAssembly.outputPath` is actually not on the disk
                // while it is in the staging folder because OnPostBuildPlayerScriptDLLs is being called once the files are already
                // transferred to the staging folder, so we are going to work from it but we are reusing the file names that we got earlier
                var playerAssemblyPathToStaging = Path.Combine(stagingFolder, Path.GetFileName(playerAssembly.outputPath));
                if (!File.Exists(playerAssemblyPathToStaging))
                {
                    Debug.LogWarning($"Unable to find player assembly: {playerAssemblyPathToStaging}");
                }
                else
                {
                    rootAssemblies.Add(playerAssemblyPathToStaging);
                    assemblyDefines.UnionWith(playerAssembly.defines);
                }
            }

            commonOptions.AddRange(assemblyFolders.Select(folder => GetOption(OptionAotAssemblyFolder, folder)));
            commonOptions.AddRange(assemblyDefines.Select(define => GetOption(OptionCompilationDefines, define)));

            // --------------------------------------------------------------------------------------------------------
            // 3) Calculate the different target CPU combinations for the specified OS
            //
            // Typically, on some platforms like iOS we can be asked to compile a ARM32 and ARM64 CPU version
            // --------------------------------------------------------------------------------------------------------
            var combinations = CollectCombinations(targetPlatform, targetCpus, report);

            // --------------------------------------------------------------------------------------------------------
            // 4) Compile each combination
            //
            // Here bcl.exe is called for each target CPU combination
            // --------------------------------------------------------------------------------------------------------

            string debugLogFile = null;
            if (BurstLoader.IsDebugging)
            {
                // Reset log files
                try
                {
                    var logDir = Path.Combine(Environment.CurrentDirectory, "Logs");
                    debugLogFile = Path.Combine(logDir, "burst_bcl_editor.log");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    File.WriteAllText(debugLogFile, string.Empty);
                }
                catch
                {
                    debugLogFile = null;
                }
            }

            if ((report.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                CreateFolderForMiscFiles(burstMiscAlongsidePath);
            }

            // Log the targets generated by BurstReflection.FindExecuteMethods
            foreach (var combination in combinations)
            {
                // Gets the output folder
                var stagingOutputFolder = Path.GetFullPath(Path.Combine(TempStaging, combination.OutputPath));
                var outputFilePrefix = Path.Combine(stagingOutputFolder, combination.LibraryName);

                var options = new List<string>(commonOptions)
                {
                    GetOption(OptionAotOutputPath, outputFilePrefix),
                    GetOption(OptionTempDirectory, Path.Combine(Environment.CurrentDirectory, "Temp", "Burst"))
                };

                foreach (var cpu in combination.TargetCpus.Cpus)
                {
                    options.Add(GetOption(OptionTarget, cpu));
                }

                if (targetPlatform == TargetPlatform.iOS || targetPlatform == TargetPlatform.tvOS || targetPlatform == TargetPlatform.Switch)
                {
                    options.Add(GetOption(OptionStaticLinkage));
                }

                if (targetPlatform == TargetPlatform.Windows)
                {
                    options.Add(GetOption(OptionLinkerOptions, $"PdbAltPath=\"{PlayerSettings.productName}_{combination.OutputPath}\""));
                }

                // finally add method group options
                options.AddRange(rootAssemblies.Select(path => GetOption(OptionRootAssembly, path)));

                // Set the flag to print a message on missing MonoPInvokeCallback attribute on IL2CPP only
                if (PlayerSettings.GetScriptingBackend(BuildPipeline.GetBuildTargetGroup(buildTarget)) == ScriptingImplementation.IL2CPP)
                {
                    options.Add(GetOption(OptionPrintLogOnMissingPInvokeCallbackAttribute));
                }

                // Log the targets generated by BurstReflection.FindExecuteMethods
                if (BurstLoader.IsDebugging && debugLogFile != null)
                {
                    try
                    {
                        var writer = new StringWriter();
                        writer.WriteLine("-----------------------------------------------------------");
                        writer.WriteLine("Combination: " + combination);
                        writer.WriteLine("-----------------------------------------------------------");

                        foreach (var option in options)
                        {
                            writer.WriteLine(option);
                        }

                        writer.WriteLine("Assemblies in AssemblyFolders:");
                        foreach (var assemblyFolder in assemblyFolders)
                        {
                            writer.WriteLine("|- Folder: " + assemblyFolder);
                            foreach (var assemblyOrDll in Directory.EnumerateFiles(assemblyFolder, "*.dll"))
                            {
                                var fileInfo = new FileInfo(assemblyOrDll);
                                writer.WriteLine("   |- " + assemblyOrDll +  " Size: " + fileInfo.Length + " Date: " + fileInfo.LastWriteTime);
                            }
                        }

                        File.AppendAllText(debugLogFile, writer.ToString());
                    }
                    catch
                    {
                        // ignored
                    }
                }

                // Allow burst to find managed symbols in the backup location in case the symbols are stripped in the build location
                options.Add(GetOption(OptionAotPdbSearchPaths, TempManagedSymbols));

                if (isDevelopmentBuild && Environment.GetEnvironmentVariable("UNITY_BURST_ENABLE_SAFETY_CHECKS_IN_PLAYER_BUILD") != null)
                {
                    options.Add("--global-safety-checks-setting=ForceOn");
                }

                options.Add(GetOption(OptionGenerateLinkXml, Path.Combine("Temp", BurstLinkXmlName)));

                // Write current options to the response file
                var responseFile = Path.GetTempFileName();
                File.WriteAllLines(responseFile, options);

                if (BurstLoader.IsDebugging)
                {
                    Debug.Log($"bcl @{responseFile}\n\nResponse File:\n" + string.Join("\n", options));
                }

                try
                {
                    string extraGlobalOptions = "";

                    if (!string.IsNullOrWhiteSpace(aotSettingsForTarget.DisabledWarnings))
                    {
                        extraGlobalOptions += GetOption(OptionDisableWarnings, aotSettingsForTarget.DisabledWarnings) + " ";
                    }

                    if (isDevelopmentBuild || aotSettingsForTarget.EnableDebugInAllBuilds)
                    {
                        if (!isDevelopmentBuild)
                        {
                            Debug.LogWarning("Symbols are being generated for burst compiled code, please ensure you intended this - see Burst AOT settings.");
                        }

                        extraGlobalOptions += GetOption(OptionDebug, "Full") + " ";
                    }

                    if (!aotSettingsForTarget.EnableOptimisations)
                    {
                        extraGlobalOptions += GetOption(OptionDisableOpt) + " ";
                    }

                    if (aotSettingsForTarget.UsePlatformSDKLinker)
                    {
                        extraGlobalOptions += GetOption(OptionAotUsePlatformSDKLinkers) + " ";
                    }

                    switch (aotSettingsForTarget.OptimizeFor)
                    {
                        case OptimizeFor.Default:
                        case OptimizeFor.Balanced:
                            extraGlobalOptions += GetOption(OptionOptLevel, 2) + " ";
                            break;
                        case OptimizeFor.Performance:
                            extraGlobalOptions += GetOption(OptionOptLevel, 3) + " ";
                            break;
                        case OptimizeFor.Size:
                            extraGlobalOptions += GetOption(OptionOptForSize) + " ";
                            extraGlobalOptions += GetOption(OptionOptLevel, 3) + " ";
                            break;
                        case OptimizeFor.FastCompilation:
                            extraGlobalOptions += GetOption(OptionOptLevel, 1) + " ";
                            break;
                    }

                    BclRunner.RunManagedProgram(Path.Combine(BurstLoader.RuntimePath, BurstAotCompilerExecutable),
                        $"{extraGlobalOptions} {BclRunner.EscapeForShell("@" + responseFile)}",
                        new BclOutputErrorParser());

                    // Additionally copy the pdb to the root of the player build so run in editor also locates the symbols
                    var pdbPath = $"{Path.Combine(stagingOutputFolder, combination.LibraryName)}.pdb";
                    if (File.Exists(pdbPath))
                    {
                        var dstPath = Path.Combine(TempStaging, $"{combination.LibraryName}.pdb");
                        File.Copy(pdbPath, dstPath, overwrite: true);
                    }
                }
                catch (BuildFailedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new BuildFailedException(e);
                }
            }

            PostProcessCombinations(targetPlatform, combinations, report);

            // Finally move out any symbols/misc files from the final output
            if ((report.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                CollateMiscFiles(combinations, burstMiscAlongsidePath, isDevelopmentBuild);
            }
        }

        private static void CreateFolderForMiscFiles(string finalFolder)
        {
            try
            {
                if (Directory.Exists(finalFolder)) Directory.Delete(finalFolder,true);
            }
            catch
            {
            }
            Directory.CreateDirectory(finalFolder);
        }

        private static void CollateMiscFiles(List<BurstOutputCombination> combinations, string finalFolder, bool retainPdbs)
        {
            foreach (var combination in combinations)
            {
                var inputPath = Path.GetFullPath(Path.Combine(TempStaging, combination.OutputPath));
                var outputPath = Path.Combine(finalFolder, combination.OutputPath);
                Directory.CreateDirectory(outputPath);
                var files = Directory.GetFiles(inputPath);
                var directories = Directory.GetDirectories(inputPath);
                foreach (var fileName in files)
                {
                    var lowerCase = fileName.ToLower();
                    if ( (!retainPdbs && lowerCase.EndsWith(".pdb")) || lowerCase.EndsWith(".dsym") || lowerCase.EndsWith(".txt"))
                    {
                        // Move the file out of the staging area so its not included in the build
                        File.Move(fileName, Path.Combine(outputPath, Path.GetFileName(fileName)));
                    }
                }
                foreach (var fileName in directories)
                {
                    var lowerCase = fileName.ToLower();
                    if ( (!retainPdbs && lowerCase.EndsWith(".pdb")) || lowerCase.EndsWith(".dsym") || lowerCase.EndsWith(".txt"))
                    {
                        // Move the folder out of the staging area so its not included in the build
                        Directory.Move(fileName, Path.Combine(outputPath, Path.GetFileName(fileName)));
                    }
                }
            }
        }

        private static bool AndroidHasX86(AndroidArchitecture architecture)
        {
            // Deal with rename that occured
            AndroidArchitecture val;
            if (AndroidArchitecture.TryParse("X86", out val))
            {
                return (architecture & val)!=0;
            }
            else if (AndroidArchitecture.TryParse("x86", out val))
            {
                return (architecture & val)!=0;
            }
            return false;
        }
        private static bool AndroidHasX86_64(AndroidArchitecture architecture)
        {
            // Deal with rename that occured
            AndroidArchitecture val;
            if (AndroidArchitecture.TryParse("X86_64", out val))
            {
                return (architecture & val)!=0;
            }
            else if (AndroidArchitecture.TryParse("x86_64", out val))
            {
                return (architecture & val)!=0;
            }
            return false;
        }

        private enum SimulatorPlatforms
        {
            iOS,
            tvOS
        }
        private static bool IsForSimulator(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.iOS:
                    return IsForSimulator(SimulatorPlatforms.iOS);
                case BuildTarget.tvOS:
                    return IsForSimulator(SimulatorPlatforms.tvOS);
                default:
                    return false;
            }
        }
        private static bool IsForSimulator(TargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case TargetPlatform.iOS:
                    return IsForSimulator(SimulatorPlatforms.iOS);
                case TargetPlatform.tvOS:
                    return IsForSimulator(SimulatorPlatforms.tvOS);
                default:
                    return false;
            }
        }
        private static bool IsForSimulator(SimulatorPlatforms simulatorPlatforms)
        {
            switch (simulatorPlatforms)
            {
                case SimulatorPlatforms.iOS:
                    return UnityEditor.PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;
                case SimulatorPlatforms.tvOS:
                    return UnityEditor.PlayerSettings.tvOS.sdkVersion == tvOSSdkVersion.Simulator;
            }

            return false;
        }

        /// <summary>
        /// Collect CPU combinations for the specified TargetPlatform and TargetCPU
        /// </summary>
        /// <param name="targetPlatform">The target platform (e.g Windows)</param>
        /// <param name="targetCpus">The target CPUs (e.g X64_SSE4)</param>
        /// <param name="report">Error reporting</param>
        /// <returns>The list of CPU combinations</returns>
        private static List<BurstOutputCombination> CollectCombinations(TargetPlatform targetPlatform, TargetCpus targetCpus, BuildReport report)
        {
            var combinations = new List<BurstOutputCombination>();

            if (targetPlatform == TargetPlatform.macOS)
            {
                // NOTE: OSX has a special folder for the plugin
                // Declared in GetStagingAreaPluginsFolder
                // PlatformDependent\OSXPlayer\Extensions\Managed\OSXDesktopStandalonePostProcessor.cs
#if UNITY_2019_3_OR_NEWER
                var outputPath = Path.Combine(Path.GetFileName(report.summary.outputPath), "Contents", "Plugins");
#else
                var outputPath = "UnityPlayer.app/Contents/Plugins";
#endif

#if UNITY_2020_2_OR_NEWER
                // Based on : PlatformDependent/OSXPlayer/Extension/OSXStandaloneBuildWindowExtension.cs
                var aotSettings = BurstPlatformAotSettings.GetOrCreateSettings(BuildTarget.StandaloneOSX);
                var buildTargetName = BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneOSX);
                var architecture = EditorUserBuildSettings.GetPlatformSettings(buildTargetName, "Architecture").ToLowerInvariant();
                switch (architecture)
                {
                    case "x64":
                        combinations.Add(new BurstOutputCombination(outputPath, aotSettings.GetDesktopCpu64Bit()));
                        break;
                    case "arm64":
                        combinations.Add(new BurstOutputCombination(outputPath, new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64)));
                        break;
                    default:
                        combinations.Add(new BurstOutputCombination(Path.Combine(outputPath, "x64"), aotSettings.GetDesktopCpu64Bit()));
                        combinations.Add(new BurstOutputCombination(Path.Combine(outputPath, "arm64"), new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64)));
                        break;
                }
#else
                combinations.Add(new BurstOutputCombination(outputPath, targetCpus));
#endif
            }
            else if (targetPlatform == TargetPlatform.iOS || targetPlatform == TargetPlatform.tvOS)
            {
                if (IsForSimulator(targetPlatform))
                {
                    Debug.LogWarning("Burst Does not currently support the simulator, burst is disabled for this build.");
                }
                else if (Application.platform != RuntimePlatform.OSXEditor)
                {
                    Debug.LogWarning("Burst Cross Compilation to iOS/tvOS for standalone player, is only supported on OSX Editor at this time, burst is disabled for this build.");
                }
                else
                {
                    var targetArchitecture = (IOSArchitecture) UnityEditor.PlayerSettings.GetArchitecture(report.summary.platformGroup);
                    if (targetArchitecture == IOSArchitecture.ARMv7 || targetArchitecture == IOSArchitecture.Universal)
                    {
                        // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                        combinations.Add(new BurstOutputCombination("StaticLibraries", new TargetCpus(BurstTargetCpu.ARMV7A_NEON32), DefaultLibraryName + "32"));
                    }

                    if (targetArchitecture == IOSArchitecture.ARM64 || targetArchitecture == IOSArchitecture.Universal)
                    {
                        // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                        combinations.Add(new BurstOutputCombination("StaticLibraries", new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64), DefaultLibraryName + "64"));
                    }
                }
            }
            else if (targetPlatform == TargetPlatform.Android)
            {
                // TODO: would be better to query AndroidNdkRoot (but thats not exposed from unity)
                string ndkRoot = null;
                var targetAPILevel = PlayerSettings.Android.GetMinTargetAPILevel();
#if UNITY_2019_3_OR_NEWER && UNITY_ANDROID
                ndkRoot = UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath;
#elif UNITY_2019_1_OR_NEWER
                // 2019.1 now has an embedded ndk
                if (EditorPrefs.HasKey("NdkUseEmbedded"))
                {
                    if (EditorPrefs.GetBool("NdkUseEmbedded"))
                    {
                        ndkRoot = Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "NDK");
                    }
                    else
                    {
                        ndkRoot = EditorPrefs.GetString("AndroidNdkRootR16b");
                    }
                }
#elif UNITY_2018_3_OR_NEWER
                // Unity 2018.3 is using NDK r16b
                ndkRoot = EditorPrefs.GetString("AndroidNdkRootR16b");
#endif

                // If we still don't have a valid root, try the old key
                if (string.IsNullOrEmpty(ndkRoot))
                {
                    ndkRoot = EditorPrefs.GetString("AndroidNdkRoot");
                }

                // Verify the directory at least exists, if not we fall back to ANDROID_NDK_ROOT current setting
                if (!string.IsNullOrEmpty(ndkRoot) && !Directory.Exists(ndkRoot))
                {
                    ndkRoot = null;
                }

                // Always set the ANDROID_NDK_ROOT (if we got a valid result from above), so BCL knows where to find the Android toolchain and its the one the user expects
                if (!string.IsNullOrEmpty(ndkRoot))
                {
                    Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndkRoot);
                }

                Environment.SetEnvironmentVariable("BURST_ANDROID_MIN_API_LEVEL", $"{targetAPILevel}");

                // Setting tempburstlibs/ as the interim target directory
                // Don't target libs/ directly because incremental build pipeline doesn't expect the so's at that path
                // Rather, so's are copied to the actual location in the gradle project in BurstAndroidGradlePostprocessor
                var androidTargetArch = PlayerSettings.Android.targetArchitectures;
                if ((androidTargetArch & AndroidArchitecture.ARMv7) != 0)
                {
                    combinations.Add(new BurstOutputCombination("tempburstlibs/armeabi-v7a", new TargetCpus(BurstTargetCpu.ARMV7A_NEON32)));
                }

                if ((androidTargetArch & AndroidArchitecture.ARM64) != 0)
                {
                    combinations.Add(new BurstOutputCombination("tempburstlibs/arm64-v8a", new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64)));
                }
#if !UNITY_2019_2_OR_NEWER
                if (AndroidHasX86(androidTargetArch))
                {
                    combinations.Add(new BurstOutputCombination("tempburstlibs/x86", new TargetCpus(BurstTargetCpu.X86_SSE2)));
                }
#endif
#if UNITY_2021_2_OR_NEWER
                if (AndroidHasX86(androidTargetArch))
                {
                    combinations.Add(new BurstOutputCombination("tempburstlibs/x86", new TargetCpus(BurstTargetCpu.X86_SSE4)));
                }
                if (AndroidHasX86_64(androidTargetArch))
                {
                    combinations.Add(new BurstOutputCombination("tempburstlibs/x86_64", new TargetCpus(BurstTargetCpu.X64_SSE4)));
                }
#endif
            }
            else if (targetPlatform == TargetPlatform.UWP)
            {
                var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(report.summary.platform);

#if UNITY_2019_1_OR_NEWER
                if (EditorUserBuildSettings.wsaUWPBuildType == WSAUWPBuildType.ExecutableOnly)
                {
                    combinations.Add(new BurstOutputCombination($"Plugins/{GetUWPTargetArchitecture()}", targetCpus));
                }
                else
#endif
                {
                    combinations.Add(new BurstOutputCombination("Plugins/x64", aotSettingsForTarget.GetDesktopCpu64Bit()));
                    combinations.Add(new BurstOutputCombination("Plugins/x86", aotSettingsForTarget.GetDesktopCpu32Bit()));
                    combinations.Add(new BurstOutputCombination("Plugins/ARM", new TargetCpus(BurstTargetCpu.THUMB2_NEON32)));
                    combinations.Add(new BurstOutputCombination("Plugins/ARM64", new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64)));
                }
            }
            else if (targetPlatform == TargetPlatform.Lumin)
            {
                // Set the LUMINSDK_UNITY so bcl.exe will be able to find the SDK
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LUMINSDK_UNITY")))
                {
                    var sdkRoot = EditorPrefs.GetString("LuminSDKRoot");
                    if (!string.IsNullOrEmpty(sdkRoot))
                    {
                        Environment.SetEnvironmentVariable("LUMINSDK_UNITY", sdkRoot);
                    }
                }
                combinations.Add(new BurstOutputCombination("Data/Plugins/", targetCpus));
            }
            else if (targetPlatform == TargetPlatform.Switch)
            {
                combinations.Add(new BurstOutputCombination("NativePlugins/", targetCpus));
            }
#if UNITY_2019_3_OR_NEWER
            else if (targetPlatform == TargetPlatform.Stadia)
            {
                combinations.Add(new BurstOutputCombination("NativePlugins", targetCpus));
            }
#endif
            else
            {
#if UNITY_2019_3_OR_NEWER
                if (targetPlatform == TargetPlatform.Windows)
                {
                    // This is what is expected by PlatformDependent\Win\Plugins.cpp
                    if (targetCpus.IsX86())
                    {
                        combinations.Add(new BurstOutputCombination("Data/Plugins/x86", targetCpus));
                    }
                    else
                    {
                        combinations.Add(new BurstOutputCombination("Data/Plugins/x86_64", targetCpus));
                    }
                }
                else
#endif
                {
                    // Safeguard
                    combinations.Add(new BurstOutputCombination("Data/Plugins/", targetCpus));
                }
            }

            return combinations;
        }

        private void PostProcessCombinations(TargetPlatform targetPlatform, List<BurstOutputCombination> combinations, BuildReport report)
        {
#if UNITY_2020_2_OR_NEWER
            if (targetPlatform == TargetPlatform.macOS && combinations.Count > 1)
            {
                // Figure out which files we need to lipo
                string outputSymbolsDir = null;
                var outputDir = Path.Combine(TempStaging, Path.GetFileName(report.summary.outputPath), "Contents", "Plugins");

                var sliceCount = combinations.Count;
                var binarySlices = new string[sliceCount];
                var debugSymbolSlices = new string[sliceCount];

                for (int i = 0; i < sliceCount; i++)
                {
                    var slice = combinations[i];

                    var binaryFileName = slice.LibraryName + ".bundle";
                    var binaryPath = Path.Combine(TempStaging, slice.OutputPath, binaryFileName);
                    binarySlices[i] = binaryPath;

                    // Only attempt to lipo symbols if they actually exist
                    var dsymPath = binaryPath + ".dsym";
                    var debugSymbolsPath = Path.Combine(dsymPath, "Contents", "Resources", "DWARF", binaryFileName);
                    if (File.Exists(debugSymbolsPath))
                    {
                        if (string.IsNullOrWhiteSpace(outputSymbolsDir))
                        {
                            // Copy over the symbols from the first combination for metadata files which we aren't merging, like Info.plist
                            var outputDsymPath = Path.Combine(outputDir, binaryFileName + ".dsym");
                            FileUtil.CopyFileOrDirectory(dsymPath, outputDsymPath);

                            outputSymbolsDir = Path.Combine(outputDsymPath, "Contents", "Resources", "DWARF");
                        }

                        debugSymbolSlices[i] = debugSymbolsPath;
                    }
                }

                // lipo combinations together
                var outBinaryFileName = combinations[0].LibraryName + ".bundle";
                RunLipo(binarySlices, Path.Combine(outputDir, outBinaryFileName));

                if (!string.IsNullOrWhiteSpace(outputSymbolsDir))
                    RunLipo(debugSymbolSlices, Path.Combine(outputSymbolsDir, outBinaryFileName));

                // Remove single-slice binary so they don't end up in the build
                for (int i = 0; i < sliceCount; i++)
                    FileUtil.DeleteFileOrDirectory(Path.GetDirectoryName(binarySlices[i]));

                // Since we have combined the files, we need to adjust combinations for the next step
                var outFolder = Path.GetDirectoryName(combinations[0].OutputPath);  // remove platform folder
                combinations.Clear();
                combinations.Add(new BurstOutputCombination(outFolder, new TargetCpus()));
            }
#endif
        }

        private static void RunLipo(string[] inputFiles, string outputFile)
        {
            var outputDir = Path.GetDirectoryName(outputFile);
            Directory.CreateDirectory(outputDir);

            var cmdLine = new StringBuilder();
            foreach (var input in inputFiles)
            {
                if (string.IsNullOrEmpty(input))
                    continue;

                cmdLine.Append(BclRunner.EscapeForShell(input));
                cmdLine.Append(' ');
            }

            cmdLine.Append("-create -output ");
            cmdLine.Append(BclRunner.EscapeForShell(outputFile));

            string lipoPath;

            var currentEditorPlatform = Application.platform;
            switch (currentEditorPlatform)
            {
                case RuntimePlatform.LinuxEditor:
                    lipoPath = Path.Combine(BurstLoader.RuntimePath, "hostlin", "llvm-lipo");
                    break;

                case RuntimePlatform.OSXEditor:
                    lipoPath = Path.Combine(BurstLoader.RuntimePath, "hostmac", "llvm-lipo");
                    break;

                case RuntimePlatform.WindowsEditor:
                    lipoPath = Path.Combine(BurstLoader.RuntimePath, "hostwin", "llvm-lipo.exe");
                    break;

                default:
                    throw new NotSupportedException("Unknown Unity editor platform: " + currentEditorPlatform);
            }

            BclRunner.RunNativeProgram(lipoPath, cmdLine.ToString(), null);
        }

        private static Assembly[] GetPlayerAssemblies(BuildReport report)
        {
            // We need to build the list of root assemblies based from the "PlayerScriptAssemblies" folder.
            // This is so we compile the versions of the library built for the individual platforms, not the editor version.
            var oldOutputDir = EditorCompilationInterface.GetCompileScriptsOutputDirectory();
            try
            {
                EditorCompilationInterface.SetCompileScriptsOutputDirectory(LibraryPlayerScriptAssemblies);

                var shouldIncludeTestAssemblies = report.summary.options.HasFlag(BuildOptions.IncludeTestAssemblies);

#if UNITY_2021_1_OR_NEWER
                // Workaround that with 'Server Build' ticked in the build options, since there is no 'AssembliesType.Server'
                // enum, we need to manually add the BuildingForHeadlessPlayer compilation option.

#if UNITY_2021_2_OR_NEWER
                // A really really really gross hack - thanks Cristian Mazo! Querying the BuildOptions.EnableHeadlessMode is
                // obselete, but accessing its integer value is not... Note: this is just the temporary workaround to unblock
                // us (as of 1st June 2021, I say this with **much hope** that it is indeed temporary!).
                var flag = (BuildOptions)16384;
#else
                var flag = BuildOptions.EnableHeadlessMode;
#endif
                if (report.summary.options.HasFlag(flag))
                {
                    var compilationOptions = EditorCompilationInterface.GetAdditionalEditorScriptCompilationOptions();
                    compilationOptions |= EditorScriptCompilationOptions.BuildingForHeadlessPlayer;

                    if (shouldIncludeTestAssemblies)
                    {
                        compilationOptions |= EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
                    }

                    return CompilationPipeline.ToAssemblies(CompilationPipeline.GetScriptAssemblies(EditorCompilationInterface.Instance, compilationOptions));
                }
                else
                {
                    return CompilationPipeline.GetAssemblies(shouldIncludeTestAssemblies ? AssembliesType.Player : AssembliesType.PlayerWithoutTestAssemblies);
                }
#elif UNITY_2019_3_OR_NEWER
                // Workaround that with 'Server Build' ticked in the build options, since there is no 'AssembliesType.Server'
                // enum, we need to manually add the 'UNITY_SERVER' define to the player assembly search list.
                if (report.summary.options.HasFlag(BuildOptions.EnableHeadlessMode))
                {
                    var compilationOptions = EditorCompilationInterface.GetAdditionalEditorScriptCompilationOptions();
                    if (shouldIncludeTestAssemblies)
                    {
                        compilationOptions |= EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
                    }

                    return CompilationPipeline.GetPlayerAssemblies(EditorCompilationInterface.Instance, compilationOptions, new string[] { "UNITY_SERVER" });
                }
                else
                {
                    return CompilationPipeline.GetAssemblies(shouldIncludeTestAssemblies ? AssembliesType.Player : AssembliesType.PlayerWithoutTestAssemblies);
                }
#else
                var compilationOptions = EditorCompilationInterface.GetAdditionalEditorScriptCompilationOptions();
                if (shouldIncludeTestAssemblies)
                {
                    compilationOptions |= EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
                }

#if UNITY_2019_2_OR_NEWER
                return CompilationPipeline.GetPlayerAssemblies(EditorCompilationInterface.Instance, compilationOptions, null);
#else
                return CompilationPipeline.GetPlayerAssemblies(EditorCompilationInterface.Instance, compilationOptions);
#endif
#endif
            }
            finally
            {
                EditorCompilationInterface.SetCompileScriptsOutputDirectory(oldOutputDir);  // restore output directory back to original value
            }
        }

        private static bool IsMonoReferenceAssemblyDirectory(string path)
        {
            var editorDir = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return path.IndexOf(editorDir, StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("MonoBleedingEdge", StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("-api", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static bool IsDotNetStandardAssemblyDirectory(string path)
        {
            var editorDir = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return path.IndexOf(editorDir, StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("shims", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static TargetPlatform GetTargetPlatformAndDefaultCpu(BuildTarget target, out TargetCpus targetCpu)
        {
            var platform = TryGetTargetPlatform(target, out targetCpu);
            if (!platform.HasValue)
            {
                throw new NotSupportedException("The target platform " + target + " is not supported by the burst compiler");
            }
            return platform.Value;
        }

        private static bool IsSupportedPlatform(BuildTarget target)
        {
            return TryGetTargetPlatform(target, out var _).HasValue;
        }

        private static TargetPlatform? TryGetTargetPlatform(BuildTarget target, out TargetCpus targetCpus)
        {
            var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(target);

            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                    targetCpus = aotSettingsForTarget.GetDesktopCpu32Bit();
                    return TargetPlatform.Windows;
                case BuildTarget.StandaloneWindows64:
                    targetCpus = aotSettingsForTarget.GetDesktopCpu64Bit();
                    return TargetPlatform.Windows;
                case BuildTarget.StandaloneOSX:
                    targetCpus = aotSettingsForTarget.GetDesktopCpu64Bit();
                    return TargetPlatform.macOS;
#if !UNITY_2019_2_OR_NEWER
                // 32 bit linux support was deprecated
                case BuildTarget.StandaloneLinux:
                    targetCpus = aotSettingsForTarget.GetDesktopCpu32Bit();
                    return TargetPlatform.Linux;
#endif
                case BuildTarget.StandaloneLinux64:
                    targetCpus = aotSettingsForTarget.GetDesktopCpu64Bit();
                    return TargetPlatform.Linux;
                case BuildTarget.WSAPlayer:
                    {
                        var uwpArchitecture = GetUWPTargetArchitecture();
                        if (string.Equals(uwpArchitecture, "x64", StringComparison.OrdinalIgnoreCase))
                        {
                            targetCpus = aotSettingsForTarget.GetDesktopCpu64Bit();
                        }
                        else if (string.Equals(uwpArchitecture, "x86", StringComparison.OrdinalIgnoreCase))
                        {
                            targetCpus = aotSettingsForTarget.GetDesktopCpu32Bit();
                        }
                        else if (string.Equals(uwpArchitecture, "ARM", StringComparison.OrdinalIgnoreCase))
                        {
                            targetCpus = new TargetCpus(BurstTargetCpu.THUMB2_NEON32);
                        }
                        else if (string.Equals(uwpArchitecture, "ARM64", StringComparison.OrdinalIgnoreCase))
                        {
                            targetCpus = new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64);
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown UWP CPU architecture: " + uwpArchitecture);
                        }

                        return TargetPlatform.UWP;
                    }
                case BuildTarget.XboxOne:
                    targetCpus = new TargetCpus(BurstTargetCpu.X64_SSE4);
                    return TargetPlatform.XboxOne;
#if UNITY_2019_4_OR_NEWER
                case BuildTarget.GameCoreXboxOne:
                    targetCpus = new TargetCpus(BurstTargetCpu.AVX);
                    return TargetPlatform.GameCoreXboxOne;
                case BuildTarget.GameCoreXboxSeries:
                    targetCpus = new TargetCpus(BurstTargetCpu.AVX2);
                    return TargetPlatform.GameCoreXboxSeries;
#endif
                case BuildTarget.PS4:
                    targetCpus = new TargetCpus(BurstTargetCpu.X64_SSE4);
                    return TargetPlatform.PS4;
                case BuildTarget.Android:
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV7A_NEON32);
                    return TargetPlatform.Android;
                case BuildTarget.iOS:
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV7A_NEON32);
                    return TargetPlatform.iOS;
                case BuildTarget.tvOS:
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64);
                    return TargetPlatform.tvOS;
                case BuildTarget.Lumin:
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64);
                    return TargetPlatform.Lumin;
                case BuildTarget.Switch:
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64);
                    return TargetPlatform.Switch;
#if UNITY_2019_3_OR_NEWER
                case BuildTarget.Stadia:
                    targetCpus = new TargetCpus(BurstTargetCpu.AVX2);
                    return TargetPlatform.Stadia;
                case BuildTarget.PS5:
                    targetCpus = new TargetCpus(BurstTargetCpu.AVX2);
                    return TargetPlatform.PS5;
#endif
            }

            if (/*BuildTarget.EmbeddedLinux*/ 45 == (int)target)
            {
                //EmbeddedLinux is supported on 2019.4 (shadow branch), 2020.3 (shadow branch) and 2021.2+ (official).
                var embeddedLinuxArchitecture = GetEmbeddedLinuxTargetArchitecture();
                if ("Arm64" == embeddedLinuxArchitecture)
                {
                    targetCpus = new TargetCpus(BurstTargetCpu.ARMV8A_AARCH64);
                }
                else if ("X64" == embeddedLinuxArchitecture)
                {
                    targetCpus = new TargetCpus(BurstTargetCpu.X64_SSE2); //lowest supported for now
                }
                else if (("X86" == embeddedLinuxArchitecture) || ("Arm32" == embeddedLinuxArchitecture))
                {
                    //32bit platforms cannot be support with the current SDK/Toolchain combination.
                    //i686-embedded-linux-gnu/8.3.0\libgcc.a(_moddi3.o + _divdi3.o): contains a compressed section, but zlib is not available
                    //_moddi3.o + _divdi3.o are required by LLVM for 64bit operations on 32bit platforms.
                    throw new InvalidOperationException($"No EmbeddedLinux Burst Support on {embeddedLinuxArchitecture} architecture.");
                }
                else
                {
                    throw new InvalidOperationException("Unknown EmbeddedLinux CPU architecture: " + embeddedLinuxArchitecture);
                }
                return TargetPlatform.EmbeddedLinux;
            }

            targetCpus = new TargetCpus(BurstTargetCpu.Auto);
            return null;
        }

        /// <summary>
        /// Not exposed by Unity Editor today.
        /// This is a copy of the Architecture enum from `PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs`
        /// </summary>
        private enum IOSArchitecture
        {
            ARMv7,
            ARM64,
            Universal
        }

        private static string GetUWPTargetArchitecture()
        {
            var architecture = EditorUserBuildSettings.wsaArchitecture;

            if (string.Equals(architecture, "x64", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(architecture, "x86", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(architecture, "ARM", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(architecture, "ARM64", StringComparison.OrdinalIgnoreCase))
            {
                return architecture;
            }

            // Default to x64 if editor user build setting is garbage
            return "x64";
        }

        private static string GetEmbeddedLinuxTargetArchitecture()
        {
            var flags = System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.FlattenHierarchy;
            var property = typeof(EditorUserBuildSettings).GetProperty("selectedEmbeddedLinuxArchitecture", flags);
            if (null == property)
            {
                return "NOT_FOUND";
            }
            var value = (int)property.GetValue(null, null);
            switch (value)
            {
                case /*UnityEditor.EmbeddedLinuxArchitecture.Arm64*/ 0: return "Arm64";
                case /*UnityEditor.EmbeddedLinuxArchitecture.Arm32*/ 1: return "Arm32";
                case /*UnityEditor.EmbeddedLinuxArchitecture.X64*/   2: return "X64";
                case /*UnityEditor.EmbeddedLinuxArchitecture.X86*/   3: return "X86";
                default: return $"UNKNOWN_{value}";
            }
        }

        /// <summary>
        /// Defines an output path (for the generated code) and the target CPU
        /// </summary>
        private struct BurstOutputCombination
        {
            public readonly TargetCpus TargetCpus;
            public readonly string OutputPath;
            public readonly string LibraryName;

            public BurstOutputCombination(string outputPath, TargetCpus targetCpus, string libraryName = DefaultLibraryName)
            {
                TargetCpus = targetCpus.Clone();
                OutputPath = outputPath;
                LibraryName = libraryName;
            }

            public override string ToString()
            {
                return $"{nameof(TargetCpus)}: {TargetCpus}, {nameof(OutputPath)}: {OutputPath}, {nameof(LibraryName)}: {LibraryName}";
            }
        }

        private class BclRunner
        {
            private static readonly Regex MatchVersion = new Regex(@"com.unity.burst@(\d+.*?)[\\/]");

            public static void RunManagedProgram(string exe, string args, CompilerOutputParserBase parser)
            {
                RunManagedProgram(exe, args, Application.dataPath + "/..", parser);
            }

            private static void RunManagedProgram(
              string exe,
              string args,
              string workingDirectory,
              CompilerOutputParserBase parser)
            {
                Program p;
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    ProcessStartInfo si = new ProcessStartInfo()
                    {
                        Arguments = args,
                        CreateNoWindow = true,
                        FileName = exe
                    };
                    p = new Program(si);
                }
                else
                {
                    p = (Program) new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), (string) null, exe, args, false, null);
                }

                RunProgram(p, exe, args, workingDirectory, parser);
            }

            public static void RunNativeProgram(string exe, string args, CompilerOutputParserBase parser)
            {
                RunNativeProgram(exe, args, Application.dataPath + "/..", parser);
            }

            private static void RunNativeProgram(string exePath, string arguments, string workingDirectory, CompilerOutputParserBase parser)
            {
                // On non Windows platform, make sure that the command is executable
                // This is a workaround - occasionally the execute bits are lost from our package
                if (Application.platform != RuntimePlatform.WindowsEditor && Path.IsPathRooted(exePath))
                {
                    var escapedExePath = EscapeForShell(exePath, singleQuoteWrapped: true);
                    var shArgs = $"-c '[ ! -x {escapedExePath} ] && chmod 755 {escapedExePath}'";

                    var p = new Program(new ProcessStartInfo("sh", shArgs) { CreateNoWindow = true});
                    p.GetProcessStartInfo().WorkingDirectory = workingDirectory;
                    p.Start();
                    p.WaitForExit();
                }

                var startInfo = new ProcessStartInfo(exePath, arguments);
                startInfo.CreateNoWindow = true;

                RunProgram(new Program(startInfo), exePath, arguments, workingDirectory, parser);
            }

            public static string EscapeForShell(string s, bool singleQuoteWrapped = false)
            {
                // On Windows it's enough to enclose the path in double quotes (double quotes are not allowed in paths)
                if (Application.platform == RuntimePlatform.WindowsEditor) return $"\"{s}\"";

                // On non-windows platforms we enclose in single-quotes and escape any existing single quotes with: '\'':
                //    John's Folder => 'John'\''s Folder'
                var sb = new StringBuilder();
                var escaped = s.Replace("'", "'\\''");
                sb.Append('\'');
                sb.Append(escaped);
                sb.Append('\'');

                // If the outer-context is already wrapped in single-quotes, we need to double escape things:
                //    John's Folder => 'John'\''s Folder'
                //                  => '\''John'\''\'\'''\''s Folder'\''
                if (singleQuoteWrapped)
                {
                    // Pain
                    return sb.ToString().Replace("'", "'\\''");
                }

                return sb.ToString();
            }



            public static void RunProgram(
              Program p,
              string exe,
              string args,
              string workingDirectory,
              CompilerOutputParserBase parser)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                using (p)
                {
                    p.GetProcessStartInfo().WorkingDirectory = workingDirectory;
                    p.Start();
                    p.WaitForExit();
                    stopwatch.Stop();

                    Console.WriteLine("{0} exited after {1} ms.", (object)exe, (object)stopwatch.ElapsedMilliseconds);
                    IEnumerable<UnityEditor.Scripting.Compilers.CompilerMessage> compilerMessages = null;
                    string[] errorOutput = p.GetErrorOutput();
                    string[] standardOutput = p.GetStandardOutput();
                    if (parser != null)
                    {
                        compilerMessages = parser.Parse(errorOutput, standardOutput, true, "n/a (burst)");
                    }

                    var errorMessageBuilder = new StringBuilder();

                    if (compilerMessages != null)
                    {
                        foreach (UnityEditor.Scripting.Compilers.CompilerMessage compilerMessage in compilerMessages)
                        {
                            switch (compilerMessage.type)
                            {
                                case CompilerMessageType.Warning:
#if UNITY_2020_2_OR_NEWER
                                    Debug.LogWarning(compilerMessage.message, compilerMessage.file, compilerMessage.line, compilerMessage.column);
#else
                                    if (compilerMessage.file != "unknown")
                                    {
                                        Debug.LogWarning($"{compilerMessage.file}({compilerMessage.line},{compilerMessage.column}): {compilerMessage.message}");
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"{compilerMessage.message}");
                                    }
#endif
                                    break;
                                case CompilerMessageType.Error:
                                    Debug.LogPlayerBuildError(compilerMessage.message, compilerMessage.file, compilerMessage.line, compilerMessage.column);
                                    break;
                            }
                        }
                    }

                    if (p.ExitCode != 0)
                    {
                        // We try to output the version in the heading error if we can
                        var matchVersion = MatchVersion.Match(exe);
                        errorMessageBuilder.Append(matchVersion.Success ?
                            "Burst compiler (" + matchVersion.Groups[1].Value + ") failed running" :
                            "Burst compiler failed running");
                        errorMessageBuilder.AppendLine();
                        errorMessageBuilder.AppendLine();
                        // Don't output the path if we are not burst-debugging or the exe exist
                        if (BurstLoader.IsDebugging || !File.Exists(exe))
                        {
                            errorMessageBuilder.Append(exe).Append(" ").Append(args);
                            errorMessageBuilder.AppendLine();
                            errorMessageBuilder.AppendLine();
                        }

                        errorMessageBuilder.AppendLine("stdout:");
                        foreach (string str in standardOutput)
                            errorMessageBuilder.AppendLine(str);
                        errorMessageBuilder.AppendLine("stderr:");
                        foreach (string str in errorOutput)
                            errorMessageBuilder.AppendLine(str);

                        throw new BuildFailedException(errorMessageBuilder.ToString());
                    }
                    Console.WriteLine(p.GetAllOutput());
                }
            }
        }

        /// <summary>
        /// Internal class used to parse bcl output errors
        /// </summary>
        private class BclOutputErrorParser : CompilerOutputParserBase
        {
            // Format of an error message:
            //
            //C:\work\burst\src\Burst.Compiler.IL.Tests\Program.cs(17,9): error: Loading a managed string literal is not supported by burst
            // at Buggy.NiceBug() (at C:\work\burst\src\Burst.Compiler.IL.Tests\Program.cs:17)
            //
            //
            //                                                                [1]    [2]         [3]        [4]         [5]
            //                                                                path   line        col        type        message
            private static readonly Regex MatchLocation = new Regex(@"^(.*?)\((\d+)\s*,\s*(\d+)\):\s*([\w\s]+)\s*:\s*(.*)");

            // Matches " at "
            private static readonly Regex MatchAt = new Regex(@"^\s+at\s+");

            public override IEnumerable<UnityEditor.Scripting.Compilers.CompilerMessage> Parse(
                string[] errorOutput,
                string[] standardOutput,
                bool compilationHadFailure,
                string assemblyName)
            {
                var messages = new List<UnityEditor.Scripting.Compilers.CompilerMessage>();
                var textBuilder = new StringBuilder();
                for (var i = 0; i < errorOutput.Length; i++)
                {
                    string line = errorOutput[i];

                    var message = new UnityEditor.Scripting.Compilers.CompilerMessage {assemblyName = assemblyName};

                    // If we are able to match a location, we can decode it including the following attached " at " lines
                    textBuilder.Clear();

                    var match = MatchLocation.Match(line);
                    if (match.Success)
                    {
                        var path = match.Groups[1].Value;
                        int.TryParse(match.Groups[2].Value, out message.line);
                        int.TryParse(match.Groups[3].Value, out message.column);
                        if (match.Groups[4].Value.Contains("error"))
                        {
                            message.type = CompilerMessageType.Error;
                        }
                        else
                        {
                            message.type = CompilerMessageType.Warning;
                        }
                        message.file = !string.IsNullOrEmpty(path) ? path : "unknown";
                        // Replace '\' with '/' to let the editor open the file
                        message.file = message.file.Replace('\\', '/');

                        // Make path relative to project path path
                        var projectPath = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
                        if (projectPath != null && message.file.StartsWith(projectPath))
                        {
                            message.file = message.file.Substring(projectPath.EndsWith("/") ? projectPath.Length : projectPath.Length + 1);
                        }

                        // debug
                        // textBuilder.AppendLine("line: " + message.line + " column: " + message.column + " error: " + message.type + " file: " + message.file);
                        textBuilder.Append(match.Groups[5].Value);
                    }
                    else
                    {
                        // Don't output any blank line
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        // Otherwise we output an error, but without source location information
                        // so that at least the user can see it directly in the log errors
                        message.type = CompilerMessageType.Error;
                        message.line = 0;
                        message.column = 0;
                        message.file = "unknown";


                        textBuilder.Append(line);
                    }

                    // Collect attached location call context information ("at ...")
                    // we do it for both case (as if we have an exception in bcl we want to print this in a single line)
                    bool isFirstAt = true;
                    for (int j = i + 1; j < errorOutput.Length; j++)
                    {
                        var nextLine = errorOutput[j];

                        // Empty lines are ignored by the stack trace parser.
                        if (string.IsNullOrWhiteSpace(nextLine))
                        {
                            i++;
                            continue;
                        }

                        if (MatchAt.Match(nextLine).Success)
                        {
                            i++;
                            if (isFirstAt)
                            {
                                textBuilder.AppendLine();
                                isFirstAt = false;
                            }
                            textBuilder.AppendLine(nextLine);
                        }
                        else
                        {
                            break;
                        }
                    }
                    message.message = textBuilder.ToString();

                    messages.Add(message);
                }
                return messages;
            }

            protected override string GetErrorIdentifier()
            {
                throw new NotImplementedException(); // as we overriding the method Parse()
            }

            protected override Regex GetOutputRegex()
            {
                throw new NotImplementedException(); // as we overriding the method Parse()
            }
        }

#if UNITY_EDITOR_OSX
        private class StaticLibraryPostProcessor
        {
            private const string TempSourceLibrary = @"Temp/StagingArea/StaticLibraries";
            [PostProcessBuildAttribute(1)]
            public static void OnPostProcessBuild(BuildTarget target, string path)
            {
                // Early out if we are building for the simulator, as we don't
				//currently generate burst libraries that will work for that.
                if (IsForSimulator(target))
                {
                    return;
                }
                // We only support AOT compilation for ios from a macos host (we require xcrun and the apple tool chains)
                //for other hosts, we simply act as if burst is not being used (an error will be generated by the build aot step)
                //this keeps the behaviour consistent with how it was before static linkage was introduced
                if (target == BuildTarget.iOS)
                {
                    var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(BuildTarget.iOS);

                    // Early exit if burst is not activated
                    if (!aotSettingsForTarget.EnableBurstCompilation)
                    {
                        return;
                    }
                    PostAddStaticLibraries(path);
                }
                if (target == BuildTarget.tvOS)
                {
                    var aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(BuildTarget.tvOS);

                    // Early exit if burst is not activated
                    if (!aotSettingsForTarget.EnableBurstCompilation)
                    {
                        return;
                    }
                    PostAddStaticLibraries(path);
                }
            }

            private static void PostAddStaticLibraries(string path)
            {
                var assm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly =>
                    assembly.GetName().Name == "UnityEditor.iOS.Extensions.Xcode");
                Type PBXType = assm?.GetType("UnityEditor.iOS.Xcode.PBXProject");
                Type PBXSourceTree = assm?.GetType("UnityEditor.iOS.Xcode.PBXSourceTree");
                if (PBXType != null && PBXSourceTree != null)
                {
                    var project = Activator.CreateInstance(PBXType, null);

                    var _sGetPBXProjectPath = PBXType.GetMethod("GetPBXProjectPath");
                    var _ReadFromFile = PBXType.GetMethod("ReadFromFile");
                    var _sGetUnityTargetName = PBXType.GetMethod("GetUnityTargetName");
                    var _AddFileToBuild = PBXType.GetMethod("AddFileToBuild");
                    var _AddFile = PBXType.GetMethod("AddFile");
                    var _WriteToString = PBXType.GetMethod("WriteToString");

                    var sourcetree = new EnumConverter(PBXSourceTree).ConvertFromString("Source");

                    string sPath = (string)_sGetPBXProjectPath?.Invoke(null, new object[] { path });
                    _ReadFromFile?.Invoke(project, new object[] { sPath });

#if UNITY_2019_3_OR_NEWER
                    var _TargetGuidByName = PBXType.GetMethod("GetUnityFrameworkTargetGuid");
                    string g = (string) _TargetGuidByName?.Invoke(project, null);
#else
                    var _TargetGuidByName = PBXType.GetMethod("TargetGuidByName");
                    string tn = (string) _sGetUnityTargetName?.Invoke(null, null);
                    string g = (string) _TargetGuidByName?.Invoke(project, new object[] {tn});
#endif

                    var srcPath = TempSourceLibrary;
                    var dstPath = "Libraries";
                    var dstCopyPath = Path.Combine(path, dstPath);

                    var burstCppLinkFile = "lib_burst_generated.cpp";

                    var lib32Name = $"{DefaultLibraryName}32.a";
                    var lib64Name = $"{DefaultLibraryName}64.a";
                    var lib32SrcPath = Path.Combine(srcPath, lib32Name);
                    var lib64SrcPath = Path.Combine(srcPath, lib64Name);
                    var lib32Exists = File.Exists(lib32SrcPath);
                    var lib64Exists = File.Exists(lib64SrcPath);
                    var numLibs = (lib32Exists?1:0)+(lib64Exists?1:0);

                    if (numLibs==0)
                    {
                        return; // No libs, so don't write the cpp either
                    }

                    var libsCombine=new string [numLibs];
                    var libsIdx=0;
                    if (lib32Exists) libsCombine[libsIdx++] = lib32SrcPath;
                    if (lib64Exists) libsCombine[libsIdx++] = lib64SrcPath;

                    // Combine the static libraries into a single file to support newer xcode build systems
                    var libName = $"{DefaultLibraryName}.a";
                    RunLipo(libsCombine, Path.Combine(dstCopyPath, libName));
                    AddLibToProject(project, _AddFileToBuild, _AddFile, sourcetree, g, dstPath, libName);

                    // Additionally we need a small cpp file (weak symbols won't unfortunately override directly from the libs
                    //presumably due to link order?
                    string cppPath = Path.Combine(dstCopyPath, burstCppLinkFile);
                    File.WriteAllText(cppPath, @"
extern ""C""
{
    void Staticburst_initialize(void* );
    void* StaticBurstStaticMethodLookup(void* );

    int burst_enable_static_linkage = 1;
    void burst_initialize(void* i) { Staticburst_initialize(i); }
    void* BurstStaticMethodLookup(void* i) { return StaticBurstStaticMethodLookup(i); }
}
");
                    cppPath = Path.Combine(dstPath, burstCppLinkFile);
                    string fileg = (string)_AddFile?.Invoke(project, new object[] { cppPath, cppPath, sourcetree });
                    _AddFileToBuild?.Invoke(project, new object[] { g, fileg });

                    string pstring = (string)_WriteToString?.Invoke(project, null);
                    File.WriteAllText(sPath, pstring);
                }
            }

            private static void AddLibToProject(object project, System.Reflection.MethodInfo _AddFileToBuild, System.Reflection.MethodInfo _AddFile, object sourcetree, string g, string dstPath, string lib32Name)
            {
                string fg = (string)_AddFile?.Invoke(project,
                    new object[] { Path.Combine(dstPath, lib32Name), Path.Combine(dstPath, lib32Name), sourcetree });
                _AddFileToBuild?.Invoke(project, new object[] { g, fg });
            }
        }
#endif
    }
}
#endif
