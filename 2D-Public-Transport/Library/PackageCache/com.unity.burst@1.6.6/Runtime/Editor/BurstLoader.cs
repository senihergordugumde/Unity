#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst.LowLevel;
#if UNITY_2020_1_OR_NEWER
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
#endif
using UnityEditor;
using UnityEditor.Compilation;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Main entry point for initializing the burst compiler service for both JIT and AOT
    /// </summary>
    [InitializeOnLoad]
    internal class BurstLoader
    {
        // Cache the delegate to make sure it doesn't get collected.
        private static readonly BurstCompilerService.ExtractCompilerFlags TryGetOptionsFromMemberDelegate = TryGetOptionsFromMember;

        private static readonly object EagerCompilationLockObject = new object();
        private static readonly CancellationTokenSource EagerCompilationTokenSource = new CancellationTokenSource();
        private static List<BurstCompileTarget> _cachedCompileTargets;

        /// <summary>
        /// Gets the location to the runtime path of burst.
        /// </summary>
        public static string RuntimePath { get; private set; }

        public static bool IsDebugging { get; private set; }

        public static int DebuggingLevel { get; private set; }

        public static bool SafeShutdown { get; private set; }

        private static void VersionUpdateCheck()
        {
            var seek = "com.unity.burst@";
            var first = RuntimePath.LastIndexOf(seek);
            var last = RuntimePath.LastIndexOf(".Runtime");
            string version;
            if (first == -1 || last == -1 || last <= first)
            {
                version = "Unknown";
            }
            else
            {
                first += seek.Length;
                last -= 1;
                version = RuntimePath.Substring(first, last - first);
            }

            var result = BurstCompiler.VersionNotify(version);
            // result will be empty if we are shutting down, and thus we shouldn't popup a dialog
            if (!String.IsNullOrEmpty(result) && result != version)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.LogWarning($"[com.unity.burst] - '{result}' != '{version}'");
                }
                OnVersionChangeDetected();
            }
        }

        private static bool UnityBurstRuntimePathOverwritten(out string path)
        {
            path = Environment.GetEnvironmentVariable("UNITY_BURST_RUNTIME_PATH");
            return Directory.Exists(path);
        }

        private static void OnVersionChangeDetected()
        {
            // Write marker file to tell Burst to delete the cache at next startup.
            try
            {
                File.Create(Path.Combine(BurstCompilerOptions.DefaultCacheFolder, BurstCompilerOptions.DeleteCacheMarkerFileName)).Dispose();
            }
            catch (IOException)
            {
                // In the unlikely scenario that two processes are creating this marker file at the same time,
                // and one of them fails, do nothing because the other one has hopefully succeeded.
            }

            // Skip checking if we are using an explicit runtime path.
            if (!UnityBurstRuntimePathOverwritten(out var _))
            {
                EditorUtility.DisplayDialog("Burst Package Update Detected", "The version of Burst used by your project has changed. Please restart the Editor to continue.", "OK");
                BurstCompiler.Shutdown();
            }
        }

        static BurstLoader()
        {
            if (BurstCompilerOptions.ForceDisableBurstCompilation)
            {
                if (!BurstCompilerOptions.IsSecondaryUnityProcess)
                {
                    UnityEngine.Debug.LogWarning("[com.unity.burst] Burst is disabled entirely from the command line");
                }
                return;
            }

            // This can be setup to get more diagnostics
            var debuggingStr = Environment.GetEnvironmentVariable("UNITY_BURST_DEBUG");
            IsDebugging = debuggingStr != null;
            if (IsDebugging)
            {
                UnityEngine.Debug.LogWarning("[com.unity.burst] Extra debugging is turned on.");
                int debuggingLevel;
                int.TryParse(debuggingStr, out debuggingLevel);
                if (debuggingLevel <= 0) debuggingLevel = 1;
                DebuggingLevel = debuggingLevel;
            }

            // Try to load the runtime through an environment variable
            if (!UnityBurstRuntimePathOverwritten(out var path))
            {
                // Otherwise try to load it from the package itself
                path = Path.GetFullPath("Packages/com.unity.burst/.Runtime");
            }

            RuntimePath = path;

            if (IsDebugging)
            {
                UnityEngine.Debug.LogWarning($"[com.unity.burst] Runtime directory set to {RuntimePath}");
            }

            BurstCompilerService.Initialize(RuntimePath, TryGetOptionsFromMemberDelegate);

            // It's important that this call comes *after* BurstCompilerService.Initialize,
            // otherwise any calls from within EnsureSynchronized to BurstCompilerService,
            // such as BurstCompiler.Disable(), will silently fail.
            BurstEditorOptions.EnsureSynchronized();

            EditorApplication.quitting += OnEditorApplicationQuitting;

#if UNITY_2019_1_OR_NEWER
            CompilationPipeline.compilationStarted += OnCompilationStarted;
#endif

#if !UNITY_2021_1_OR_NEWER
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;
#endif
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

            SafeShutdown = false;
#if UNITY_2020_2_OR_NEWER
            UnityEditor.PackageManager.Events.registeringPackages += PackageRegistrationEvent;
            SafeShutdown = BurstCompiler.IsApiAvailable("SafeShutdown");
#endif

            if (!SafeShutdown)
            {
                VersionUpdateCheck();
            }

            BurstReflection.EnsureInitialized();

#if !UNITY_2019_3_OR_NEWER
            // Workaround to update the list of assembly folders as soon as possible
            // in order for the JitCompilerService to not fail with AssemblyResolveExceptions.
            // This workaround is only necessary for editors prior to 2019.3 (i.e. 2018.4),
            // because 2019.3+ include a fix on the Unity side.
            try
            {
                var assemblyList = BurstReflection.AllEditorAssemblies;
                var assemblyFolders = new HashSet<string>();
                foreach (var assembly in assemblyList)
                {
                    try
                    {
                        var fullPath = Path.GetFullPath(assembly.Location);
                        var assemblyFolder = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(assemblyFolder))
                        {
                            assemblyFolders.Add(assemblyFolder);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                // Notify the compiler
                var assemblyFolderList = assemblyFolders.ToList();
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"Burst - Change of list of assembly folders:\n{string.Join("\n", assemblyFolderList)}");
                }
                BurstCompiler.UpdateAssemblerFolders(assemblyFolderList);
            }
            catch
            {
                // ignore
            }
#endif

            // Notify the compiler about a domain reload
            if (IsDebugging)
            {
                UnityEngine.Debug.Log("Burst - Domain Reload");
            }

            // Notify the JitCompilerService about a domain reload
            BurstCompiler.DomainReload();

#if UNITY_2020_1_OR_NEWER
            BurstCompiler.OnProgress += OnProgress;
            BurstCompiler.SetProgressCallback();

            BurstCompiler.OnProfileBegin += OnProfileBegin;
            BurstCompiler.OnProfileEnd += OnProfileEnd;
            BurstCompiler.SetProfilerCallbacks();
#endif

            // Make sure BurstRuntime is initialized
            BurstRuntime.Initialize();

            // Schedule upfront compilation of all methods in all assemblies,
            // with the goal of having as many methods as possible Burst-compiled
            // by the time the user enters PlayMode.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                MaybeTriggerEagerCompilation();
            }

#if UNITY_2020_1_OR_NEWER
            // Can't call Menu.AddMenuItem immediately, presumably because the menu controller isn't initialized yet.
            EditorApplication.CallDelayed(() => CreateDynamicMenuItems());
#endif
        }

        private static bool _isQuitting;
        private static void OnEditorApplicationQuitting()
        {
            _isQuitting = true;
        }

#if UNITY_2020_2_OR_NEWER
        public static Action OnBurstShutdown;

        private static void PackageRegistrationEvent(UnityEditor.PackageManager.PackageRegistrationEventArgs obj)
        {
            bool requireCleanup = false;
            if (SafeShutdown)
            {
                foreach (var changed in obj.changedFrom)
                {
                    if (changed.name.Contains("com.unity.burst"))
                    {
                        requireCleanup = true;
                        break;
                    }
                }
            }
            foreach (var removed in obj.removed)
            {
                if (removed.name.Contains("com.unity.burst"))
                {
                    requireCleanup = true;
                }
            }

            if (requireCleanup)
            {
                OnBurstShutdown?.Invoke();
                if (!SafeShutdown)
                {
                    EditorUtility.DisplayDialog("Burst Package Has Been Removed", "Please restart the Editor to continue.", "OK");
                }
                BurstCompiler.Shutdown();
            }
        }
#endif

#if UNITY_2020_1_OR_NEWER
        // Don't initialize to 0 because that could be a valid progress ID.
        private static int BurstProgressId = -1;

        // If this enum changes, update the benchmarks tool accordingly as we rely on integer value related to this enum
        internal enum BurstEagerCompilationStatus
        {
            NotScheduled,
            Scheduled,
            Completed
        }

        // For the time being, this field is only read through reflection
        internal static BurstEagerCompilationStatus EagerCompilationStatus;

        private static void OnProgress(int current, int total)
        {
            if (current == total)
            {
                EagerCompilationStatus = BurstEagerCompilationStatus.Completed;
            }

            // OnProgress is called from a background thread,
            // but we need to update the progress UI on the main thread.
            EditorApplication.CallDelayed(() =>
            {
                if (current == total)
                {
                    // We've finished - remove progress bar.
                    if (Progress.Exists(BurstProgressId))
                    {
                        Progress.Remove(BurstProgressId);
                        BurstProgressId = -1;
                    }
                }
                else
                {
                    // Do we need to create the progress bar?
                    if (!Progress.Exists(BurstProgressId))
                    {
                        BurstProgressId = Progress.Start(
                            "Burst",
                            "Compiling...",
                            Progress.Options.Unmanaged);
                    }

                    Progress.Report(
                        BurstProgressId,
                        current / (float)total,
                        $"Compiled {current} / {total} methods");
                }
            });
        }

        [ThreadStatic]
        private static Dictionary<string, IntPtr> ProfilerMarkers;

        private static unsafe void OnProfileBegin(string markerName, string metadataName, string metadataValue)
        {
            if (ProfilerMarkers == null)
            {
                // Initialize thread-static dictionary.
                ProfilerMarkers = new Dictionary<string, IntPtr>();
            }

            if (!ProfilerMarkers.TryGetValue(markerName, out var markerPtr))
            {
                ProfilerMarkers.Add(markerName, markerPtr = ProfilerUnsafeUtility.CreateMarker(
                    markerName,
                    ProfilerUnsafeUtility.CategoryScripts,
                    MarkerFlags.Script,
                    metadataName != null ? 1 : 0));

                // metadataName is assumed to be consistent for a given markerName.
                if (metadataName != null)
                {
                    ProfilerUnsafeUtility.SetMarkerMetadata(
                        markerPtr,
                        0,
                        metadataName,
                        (byte)ProfilerMarkerDataType.String16,
                        (byte)ProfilerMarkerDataUnit.Undefined);
                }
            }

            if (metadataName != null && metadataValue != null)
            {
                fixed (char* methodNamePtr = metadataValue)
                {
                    var metadata = new ProfilerMarkerData
                    {
                        Type = (byte)ProfilerMarkerDataType.String16,
                        Size = ((uint)metadataValue.Length + 1) * 2,
                        Ptr = methodNamePtr
                    };
                    ProfilerUnsafeUtility.BeginSampleWithMetadata(markerPtr, 1, &metadata);
                }
            }
            else
            {
                ProfilerUnsafeUtility.BeginSample(markerPtr);
            }
        }

        private static void OnProfileEnd(string markerName)
        {
            if (ProfilerMarkers == null)
            {
                // If we got here it means we had a domain reload between when we called profile begin and
                // now profile end, and so we need to bail out.
                return;
            }

            if (!ProfilerMarkers.TryGetValue(markerName, out var markerPtr))
            {
                return;
            }

            ProfilerUnsafeUtility.EndSample(markerPtr);
        }
#endif

        private static void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log($"Burst - Change of Editor State: {state}");
            }

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (BurstCompiler.Options.RequiresSynchronousCompilation)
                    {
                        if (DebuggingLevel > 2)
                        {
                            UnityEngine.Debug.Log("Burst - Exiting EditMode - waiting for any pending synchronous jobs");
                        }

                        EditorUtility.DisplayProgressBar("Burst", "Waiting for synchronous compilation to finish", -1);
                        try
                        {
                            BurstCompiler.WaitUntilCompilationFinished();
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }

                        if (DebuggingLevel > 2)
                        {
                            UnityEngine.Debug.Log("Burst - Exiting EditMode - finished waiting for any pending synchronous jobs");
                        }
                    }
                    else
                    {
                        BurstCompiler.ClearEagerCompilationQueues();
                        if (DebuggingLevel > 2)
                        {
                            UnityEngine.Debug.Log("Burst - Exiting EditMode - cleared eager-compilation queues");
                        }
                    }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // If Synchronous Compilation is checked, then we will already have waited for eager-compilation to finish
                    // before entering playmode. But if it was unchecked, we may have cancelled in-progress eager-compilation.
                    // We start it again here.
                    if (!BurstCompiler.Options.RequiresSynchronousCompilation)
                    {
                        if (DebuggingLevel > 2)
                        {
                            UnityEngine.Debug.Log("Burst - Exiting PlayMode - triggering eager-compilation");
                        }

                        MaybeTriggerEagerCompilation();
                    }

                    // Cleanup any loaded burst natives so users have a clean point to update the libraries.
                    BurstCompiler.UnloadAdditionalLibraries();
                    break;
            }
        }

        private static void CancelEagerCompilationPriorToAssemblyCompilation()
        {
            BurstCompiler.CancelEagerCompilation();

            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log("Burst - Cancelled eager-compilation prior to assembly compilation");
            }
        }

#if UNITY_2019_1_OR_NEWER
        private static void OnCompilationStarted(object value)
        {
            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - compilation started for '{value}'");
            }

            CancelEagerCompilationPriorToAssemblyCompilation();
        }
#endif

        private static void OnAssemblyCompilationFinished(string arg1, CompilerMessage[] arg2)
        {
            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - Assembly compilation finished for '{arg1}'");
            }
        }

#if !UNITY_2019_1_OR_NEWER
        private static bool _hasCompilationStarted;
#endif

#if !UNITY_2021_1_OR_NEWER
        // This callback has been deprecated on 2021_1 and above
        private static void OnAssemblyCompilationStarted(string obj)
        {
            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - Assembly compilation started for '{obj}'");
            }

#if !UNITY_2019_1_OR_NEWER
            // This is a workaround for 2018.4 not having the CompilationPipeline.compilationStarted event.
            if (!_hasCompilationStarted)
            {
                CancelEagerCompilationPriorToAssemblyCompilation();
                _hasCompilationStarted = true;
            }
#endif
        }
#endif

        private static bool TryGetOptionsFromMember(MemberInfo member, out string flagsOut)
        {
            return BurstCompiler.Options.TryGetOptions(member, true, out flagsOut);
        }

        private static void MaybeTriggerEagerCompilation()
        {
            var isEagerCompilationEnabled =
                BurstCompiler.Options.IsEnabled
                && Environment.GetEnvironmentVariable("UNITY_BURST_EAGER_COMPILATION_DISABLED") == null
                && (!UnityEngine.Application.isBatchMode || Environment.GetEnvironmentVariable("UNITY_BURST_EAGER_COMPILATION_ENABLED") != null);

            if (!isEagerCompilationEnabled)
            {
                return;
            }

            // Trigger compilation only if one of the following is true:
            // 1. Unity version is 2020.1 or older, AND the CompilationPipeline.IsCodegenComplete() API exists and returns true
            // 2. Unity version is 2020.1 or older, AND the CompilationPipeline.IsCodegenComplete() API does not exist
            // 3. Unity version is 2020.2+
            //
            // Eager-compilation logging is only enabled if one of the following is true:
            // 1. Unity version is 2020.2+
            // 2. Unity version is 2020.1 or older, AND the CompilationPipeline.IsCodegenComplete() API exists and returns true
#if UNITY_2020_2_OR_NEWER
            var shouldTriggerEagerCompilation = true;
            var loggingEnabled = true;
#else
            var isCodegenCompleteMethod = typeof(CompilationPipeline).GetMethod("IsCodegenComplete", BindingFlags.NonPublic | BindingFlags.Static);
            var hasValidCodegenCompleteMethod =
                isCodegenCompleteMethod != null &&
                isCodegenCompleteMethod.GetParameters().Length == 0 &&
                isCodegenCompleteMethod.ReturnType == typeof(bool);
            var shouldTriggerEagerCompilation = true;
            var loggingEnabled = false;
            if (hasValidCodegenCompleteMethod)
            {
                try
                {
                    shouldTriggerEagerCompilation = (bool)isCodegenCompleteMethod.Invoke(null, Array.Empty<object>());
                    loggingEnabled = shouldTriggerEagerCompilation;
                    if (shouldTriggerEagerCompilation && DebuggingLevel > 2)
                    {
                        UnityEngine.Debug.Log("CompilationPipeline.IsCodegenComplete() exists and returned true");
                    }
                }
                catch (Exception ex)
                {
                    if (DebuggingLevel > 2)
                    {
                        UnityEngine.Debug.Log("CompilationPipeline.IsCodegenComplete() exists but there was an error calling it: " + ex);
                    }
                }
            }
#endif

            BurstCompiler.EagerCompilationLoggingEnabled = loggingEnabled;

            if (shouldTriggerEagerCompilation)
            {
                TriggerEagerCompilation();
            }
        }

        private static void TriggerEagerCompilation()
        {
            if (_cachedCompileTargets != null)
            {
                Task.Run(ScheduleEagerCompilation, EagerCompilationTokenSource.Token);
            }
            else
            {
                if (DebuggingLevel > 2)
                {
                    UnityEngine.Debug.Log("Burst - Finding methods for eager-compilation");
                }

                var assemblyList = BurstReflection.EditorAssembliesThatCanPossiblyContainJobsExcludingTestAssemblies;

                Task.Run(
                    () =>
                    {
                        _cachedCompileTargets = BurstReflection.FindExecuteMethods(assemblyList, BurstReflectionAssemblyOptions.ExcludeTestAssemblies).CompileTargets;

                        ScheduleEagerCompilation();
                    },
                    EagerCompilationTokenSource.Token);
            }
        }

        private static void ScheduleEagerCompilation()
        {
            lock (EagerCompilationLockObject)
            {
                if (EagerCompilationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (_cachedCompileTargets == null)
                {
                    throw new InvalidOperationException();
                }

                if (DebuggingLevel > 2)
                {
                    UnityEngine.Debug.Log($"Burst - Starting scheduling eager-compilation");
                }

                var methodsToCompile = new List<EagerCompilationRequest>();
                foreach (var compileTarget in _cachedCompileTargets)
                {
                    var member = compileTarget.IsStaticMethod
                        ? (MemberInfo)compileTarget.Method
                        : compileTarget.JobType;

                    if (BurstCompiler.Options.TryGetOptions(member, true, out var optionsString, isForEagerCompilation: true))
                    {
                        if (compileTarget.IsStaticMethod)
                        {
                            optionsString += "\n--" + BurstCompilerOptions.OptionJitIsForFunctionPointer;
                        }

                        var encodedMethod = BurstCompilerService.GetMethodSignature(compileTarget.Method);
                        methodsToCompile.Add(new EagerCompilationRequest(encodedMethod, optionsString));
                    }
                }

                BurstCompiler.EagerCompileMethods(methodsToCompile);
#if UNITY_2020_1_OR_NEWER
                EagerCompilationStatus = BurstEagerCompilationStatus.Scheduled;
#endif

                if (DebuggingLevel > 2)
                {
                    UnityEngine.Debug.Log($"Burst - Finished scheduling eager-compilation of {methodsToCompile.Count} methods");
                }
            }
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            if (DebuggingLevel > 2)
            {
                UnityEngine.Debug.Log($"Burst - OnDomainUnload");
            }

            lock (EagerCompilationLockObject)
            {
                EagerCompilationTokenSource.Cancel();
            }

            BurstCompiler.Cancel();

            // This check here is to execute shutdown after all OnDisable's. EditorApplication.quitting event is called before OnDisable's, so we need to shutdown in here.
            if (_isQuitting)
            {
                BurstCompiler.Shutdown();
            }
            
#if UNITY_2020_1_OR_NEWER
            // Because of a check in Unity (specifically SCRIPTINGAPI_THREAD_AND_SERIALIZATION_CHECK),
            // we are not allowed to call thread-unsafe methods (like Progress.Exists) after the
            // kApplicationTerminating bit has been set. And because the domain is unloaded
            // (thus triggering AppDomain.DomainUnload) *after* that bit is set, we can't call Progress.Exists
            // during shutdown. So we check _isQuitting here. When quitting, it's fine for the progress item
            // not to be removed since it's all being torn down anyway.
            if (!_isQuitting && Progress.Exists(BurstProgressId))
            {
                Progress.Remove(BurstProgressId);
                BurstProgressId = -1;
            }
#endif
        }

#if UNITY_2020_1_OR_NEWER
        private static void CreateDynamicMenuItems()
        {
            if (Unsupported.IsDeveloperMode())
            {
                Menu.AddMenuItem(
                    "Jobs/Burst/Clear JIT Cache",
                    "",
                    false,
                    1001, // Add at bottom of Burst menu, below standard items which have default priority of 1000
                    () =>
                    {
                        BurstEditorUtility.RequestClearJitCache();
                        EditorUtility.RequestScriptReload();
                    },
                    () => !EditorApplication.isPlayingOrWillChangePlaymode);
            }
        }
#endif
    }
}
#endif