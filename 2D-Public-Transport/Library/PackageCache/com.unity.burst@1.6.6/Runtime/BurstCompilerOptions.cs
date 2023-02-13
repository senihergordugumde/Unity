// BurstCompiler.Compile is not supported on Tiny/ZeroPlayer
#if !UNITY_DOTSPLAYER && !NET_DOTS
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !BURST_COMPILER_SHARED
using Unity.Jobs.LowLevel.Unsafe;
#endif

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// NOTE: This file is shared via a csproj cs link in Burst.Compiler.IL
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

#endif //!UNITY_DOTSPLAYER && !NET_DOTS
namespace Unity.Burst
{
    internal enum GlobalSafetyChecksSettingKind
    {
        Off = 0,
        On = 1,
        ForceOn = 2,
    }

#if !UNITY_DOTSPLAYER && !NET_DOTS
    /// <summary>
    /// Options available at Editor time and partially at runtime to control the behavior of the compilation and to enable/disable burst jobs.
    /// </summary>
#if BURST_COMPILER_SHARED
    internal sealed partial class BurstCompilerOptionsInternal
#else
    public sealed partial class BurstCompilerOptions
#endif
    {
        private const string DisableCompilationArg = "--burst-disable-compilation";

        private const string ForceSynchronousCompilationArg = "--burst-force-sync-compilation";

        internal const string DefaultLibraryName = "lib_burst_generated";

        internal const string BurstInitializeName = "burst.initialize";
        internal const string BurstInitializeExternalsName = "burst.initialize.externals";
        internal const string BurstInitializeStaticsName = "burst.initialize.statics";

#if BURST_COMPILER_SHARED || UNITY_EDITOR
        internal static readonly string DefaultCacheFolder = Path.Combine(Environment.CurrentDirectory, "Library", "BurstCache", "JIT");
        internal const string DeleteCacheMarkerFileName = "DeleteCache.txt";
#endif

        internal const string OptionDoNotEagerCompile = "do-not-eager-compile";
        internal const string DoNotEagerCompile = "--" + OptionDoNotEagerCompile;

        // -------------------------------------------------------
        // Common options used by the compiler
        // -------------------------------------------------------
        internal const string OptionGroup = "group";
        internal const string OptionPlatform = "platform=";
        internal const string OptionBackend = "backend=";
        internal const string OptionGlobalSafetyChecksSetting = "global-safety-checks-setting=";
        internal const string OptionDisableSafetyChecks = "disable-safety-checks";
        internal const string OptionDisableOpt = "disable-opt";
        internal const string OptionFastMath = "fastmath";
        internal const string OptionTarget = "target=";
        internal const string OptionOptLevel = "opt-level=";
        internal const string OptionOptForSize = "opt-for-size";
        internal const string OptionFloatPrecision = "float-precision=";
        internal const string OptionFloatMode = "float-mode=";
        internal const string OptionDisableWarnings = "disable-warnings=";
        internal const string OptionCompilationDefines = "compilation-defines=";
        internal const string OptionDump = "dump=";
        internal const string OptionFormat = "format=";
        internal const string OptionDebugTrap = "debugtrap";
        internal const string OptionDisableVectors = "disable-vectors";
        internal const string OptionDebug = "debug=";
        internal const string OptionDebugMode = "debugMode";
        internal const string OptionStaticLinkage = "generate-static-linkage-methods";
        internal const string OptionJobMarshalling = "generate-job-marshalling-methods";
        internal const string OptionTempDirectory = "temp-folder=";
        internal const string OptionEnableDirectExternalLinking = "enable-direct-external-linking";
        internal const string OptionLinkerOptions = "linker-options=";
        internal const string OptionEnableAutoLayoutFallbackCheck = "enable-autolayout-fallback-check";
        internal const string OptionGenerateLinkXml = "generate-link-xml=";

        // -------------------------------------------------------
        // Options used by the Jit and Bcl compilers
        // -------------------------------------------------------
        internal const string OptionCacheDirectory = "cache-directory=";

        // -------------------------------------------------------
        // Options used by the Jit compiler
        // -------------------------------------------------------
        internal const string OptionJitDisableFunctionCaching = "disable-function-caching";
        internal const string OptionJitDisableAssemblyCaching = "disable-assembly-caching";
        internal const string OptionJitEnableAssemblyCachingLogs = "enable-assembly-caching-logs";
        internal const string OptionJitEnableSynchronousCompilation = "enable-synchronous-compilation";
        internal const string OptionJitCompilationPriority = "compilation-priority=";

        // TODO: Remove this option and use proper dump flags or revisit how we log timings
        internal const string OptionJitLogTimings = "log-timings";

        internal const string OptionJitIsForFunctionPointer = "is-for-function-pointer";

        internal const string OptionJitManagedFunctionPointer = "managed-function-pointer=";

        internal const string OptionJitProvider = "jit-provider=";
        internal const string OptionJitSkipCheckDiskCache = "skip-check-disk-cache";
        internal const string OptionJitSkipBurstInitialize = "skip-burst-initialize";

        internal const string OptionEnableInterpreter = "enable-interpreter";

        // -------------------------------------------------------
        // Options used by the Aot compiler
        // -------------------------------------------------------
        internal const string OptionAotAssemblyFolder = "assembly-folder=";
        internal const string OptionRootAssembly = "root-assembly=";
        internal const string OptionIncludeRootAssemblyReferences = "include-root-assembly-references=";
        internal const string OptionAotMethod = "method=";
        internal const string OptionAotType = "type=";
        internal const string OptionAotAssembly = "assembly=";
        internal const string OptionAotOutputPath = "output=";
        internal const string OptionAotKeepIntermediateFiles = "keep-intermediate-files";
        internal const string OptionAotNoLink = "nolink";
        internal const string OptionAotPatchedAssembliesOutputFolder = "patch-assemblies-into=";
        internal const string OptionAotPinvokeNameToPatch = "pinvoke-name=";
        internal const string OptionAotExecuteMethodNameToFind = "execute-method-name=";

        internal const string OptionAotUsePlatformSDKLinkers = "use-platform-sdk-linkers";
        internal const string OptionAotOnlyStaticMethods = "only-static-methods";
        internal const string OptionMethodPrefix = "method-prefix=";
        internal const string OptionAotNoNativeToolchain = "no-native-toolchain";
        internal const string OptionAotEmitLlvmObjects = "emit-llvm-objects";
        internal const string OptionAotKeyFolder = "key-folder=";
        internal const string OptionAotDecodeFolder = "decode-folder=";
        internal const string OptionVerbose = "verbose";
        internal const string OptionValidateExternalToolChain = "validate-external-tool-chain";
        internal const string OptionCompilerThreads = "threads=";
        internal const string OptionChunkSize = "chunk-size=";
        internal const string OptionPrintLogOnMissingPInvokeCallbackAttribute = "print-monopinvokecallbackmissing-message";
        internal const string OptionOutputMode = "output-mode=";
        internal const string OptionAlwaysCreateOutput = "always-create-output=";
        internal const string OptionAotPdbSearchPaths = "pdb-search-paths=";
        internal const string OptionSafetyChecks = "safety-checks";

        internal const string CompilerCommandShutdown = "$shutdown";
        internal const string CompilerCommandCancel = "$cancel";
        internal const string CompilerCommandEnableCompiler = "$enable_compiler";
        internal const string CompilerCommandDisableCompiler = "$disable_compiler";
        internal const string CompilerCommandTriggerRecompilation = "$trigger_recompilation";
        internal const string CompilerCommandEagerCompileMethods = "$eager_compile_methods";
        internal const string CompilerCommandWaitUntilCompilationFinished = "$wait_until_compilation_finished";
        internal const string CompilerCommandClearEagerCompilationQueues = "$clear_eager_compilation_queues";
        internal const string CompilerCommandCancelEagerCompilation = "$cancel_eager_compilation";
        internal const string CompilerCommandReset = "$reset";
        internal const string CompilerCommandDomainReload = "$domain_reload";
        internal const string CompilerCommandUpdateAssemblyFolders = "$update_assembly_folders";
        internal const string CompilerCommandVersionNotification = "$version";
        internal const string CompilerCommandSetProgressCallback = "$set_progress_callback";
        internal const string CompilerCommandRequestClearJitCache = "$request_clear_jit_cache";
        internal const string CompilerCommandSetProfileCallbacks = "$set_profile_callbacks";
        internal const string CompilerCommandUnloadBurstNatives = "$unload_burst_natives";
        internal const string CompilerCommandIsNativeApiAvailable = "$is_native_api_available";
        internal const string CompilerCommandILPPCompilation = "$ilpp_compilation";
        internal const string CompilerCommandIsArmTestEnv = "$is_arm_test_env";

        // All the following content is exposed to the public interface

#if !BURST_COMPILER_SHARED
        // These fields are only setup at startup
        internal static readonly bool ForceDisableBurstCompilation;
        private static readonly bool ForceBurstCompilationSynchronously;
        internal static readonly bool IsSecondaryUnityProcess;

#if UNITY_EDITOR
        internal bool IsInitializing;
#endif

        private bool _enableBurstCompilation;
        private bool _enableBurstCompileSynchronously;
        private bool _enableBurstSafetyChecks;
        private bool _enableBurstTimings;
        private bool _enableBurstDebug;
        private bool _forceEnableBurstSafetyChecks;

        private BurstCompilerOptions() : this(false)
        {
        }

        internal BurstCompilerOptions(bool isGlobal)
        {
#if UNITY_EDITOR
            IsInitializing = true;
#endif

            try
            {
                IsGlobal = isGlobal;
                // By default, burst is enabled as well as safety checks
                EnableBurstCompilation = true;
                EnableBurstSafetyChecks = true;
            }
            finally
            {
#if UNITY_EDITOR
                IsInitializing = false;
#endif
            }
        }

        /// <summary>
        /// <c>true</c> if this option is the global options that affects menus
        /// </summary>
        private bool IsGlobal { get; }

        /// <summary>
        /// Gets a boolean indicating whether burst is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => EnableBurstCompilation && !ForceDisableBurstCompilation;
        }

        /// <summary>
        /// Gets or sets a boolean to enable or disable compilation of burst jobs.
        /// </summary>
        public bool EnableBurstCompilation
        {
            get => _enableBurstCompilation;
            set
            {
                // If we are in the global settings, and we are forcing to no burst compilation
                if (IsGlobal && ForceDisableBurstCompilation) value = false;

                bool changed = _enableBurstCompilation != value;

                if (changed && value)
                {
                    MaybePreventChangingOption();
                }

                _enableBurstCompilation = value;

                // Modify only JobsUtility.JobCompilerEnabled when modifying global settings
                if (IsGlobal)
                {
#if !BURST_INTERNAL
                    // We need also to disable jobs as functions are being cached by the job system
                    // and when we ask for disabling burst, we are also asking the job system
                    // to no longer use the cached functions
                    JobsUtility.JobCompilerEnabled = value;
#if UNITY_EDITOR
                    if (changed)
                    {
                        // Send the command to the compiler service
                        if (value)
                        {
                            BurstCompiler.Enable();
                            MaybeTriggerRecompilation();
                        }
                        else
                        {
                            BurstCompiler.Disable();
                        }
                    }
#endif
#endif

                    // Store the option directly into BurstCompiler.IsEnabled
                    BurstCompiler._IsEnabled = value;
                }

                if (changed)
                {
                    OnOptionsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean to force the compilation of all burst jobs synchronously.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool EnableBurstCompileSynchronously
        {
            get => _enableBurstCompileSynchronously;
            set
            {
                bool changed = _enableBurstCompileSynchronously != value;
                _enableBurstCompileSynchronously = value;
                if (changed) OnOptionsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a boolean to enable or disable safety checks.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool EnableBurstSafetyChecks
        {
            get => _enableBurstSafetyChecks;
            set
            {
                bool changed = _enableBurstSafetyChecks != value;

                if (changed)
                {
                    MaybePreventChangingOption();
                }

                _enableBurstSafetyChecks = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean to force enable safety checks, irrespective of what
        /// <c>EnableBurstSafetyChecks</c> is set to, or whether the job or function
        /// has <c>DisableSafetyChecks</c> set.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool ForceEnableBurstSafetyChecks
        {
            get => _forceEnableBurstSafetyChecks;
            set
            {
                bool changed = _forceEnableBurstSafetyChecks != value;

                if (changed)
                {
                    MaybePreventChangingOption();
                }

                _forceEnableBurstSafetyChecks = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }
		/// <summary>
		/// Enable debugging mode
		/// </summary>
        public bool EnableBurstDebug
        {
            get => _enableBurstDebug;
            set
            {
                bool changed = _enableBurstDebug != value;

                if (changed)
                {
                    MaybePreventChangingOption();
                }

                _enableBurstDebug = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }

        /// <summary>
        /// This property is no longer used and will be removed in a future major release.
        /// </summary>
        [Obsolete("This property is no longer used and will be removed in a future major release")]
        public bool DisableOptimizations
        {
            get => false;
            set
            {
            }
        }

        /// <summary>
        /// This property is no longer used and will be removed in a future major release. Use the [BurstCompile(FloatMode = FloatMode.Fast)] on the method directly to enable this feature
        /// </summary>
        [Obsolete("This property is no longer used and will be removed in a future major release. Use the [BurstCompile(FloatMode = FloatMode.Fast)] on the method directly to enable this feature")]
        public bool EnableFastMath
        {
            get => true;

            set
            {
                // ignored
            }
        }

        internal bool EnableBurstTimings
        {
            get => _enableBurstTimings;
            set
            {
                bool changed = _enableBurstTimings != value;
                _enableBurstTimings = value;
                if (changed) OnOptionsChanged();
            }
        }

        internal bool RequiresSynchronousCompilation => EnableBurstCompileSynchronously || ForceBurstCompilationSynchronously;

        internal Action OptionsChanged { get; set; }

        internal BurstCompilerOptions Clone()
        {
            // WARNING: for some reason MemberwiseClone() is NOT WORKING on Mono/Unity
            // so we are creating a manual clone
            var clone = new BurstCompilerOptions
            {
                EnableBurstCompilation = EnableBurstCompilation,
                EnableBurstCompileSynchronously = EnableBurstCompileSynchronously,
                EnableBurstSafetyChecks = EnableBurstSafetyChecks,
                EnableBurstTimings = EnableBurstTimings,
                EnableBurstDebug = EnableBurstDebug,
                ForceEnableBurstSafetyChecks = ForceEnableBurstSafetyChecks,
            };
            return clone;
        }

        private static bool TryGetAttribute(MemberInfo member, out BurstCompileAttribute attribute, bool isForEagerCompilation = false)
        {
            attribute = null;
            // We don't fail if member == null as this method is being called by native code and doesn't expect to crash
            if (member == null)
            {
                return false;
            }

            // Fetch options from attribute
            attribute = GetBurstCompileAttribute(member);
            if (attribute == null)
            {
                return false;
            }

            // If we're compiling for eager compilation, and this method has requested not to be eager-compiled... don't compile it.
            if (isForEagerCompilation && (attribute.Options?.Contains(DoNotEagerCompile) ?? false))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetAttribute(Assembly assembly, out BurstCompileAttribute attribute)
        {
            // We don't fail if assembly == null as this method is being called by native code and doesn't expect to crash
            if (assembly == null)
            {
                attribute = null;
                return false;
            }

            // Fetch options from attribute
            attribute = assembly.GetCustomAttribute<BurstCompileAttribute>();

            return attribute != null;
        }

        private static BurstCompileAttribute GetBurstCompileAttribute(MemberInfo memberInfo)
        {
            var result = memberInfo.GetCustomAttribute<BurstCompileAttribute>();
            if (result != null)
            {
                return result;
            }

            foreach (var a in memberInfo.GetCustomAttributes())
            {
                var attributeType = a.GetType();
                if (attributeType.FullName == "Burst.Compiler.IL.Tests.TestCompilerAttribute")
                {
                    var options = new List<string>();

                    // Don't eager-compile tests that we expect to fail compilation.
                    var expectCompilerExceptionProperty = attributeType.GetProperty("ExpectCompilerException");
                    var expectCompilerException = (expectCompilerExceptionProperty != null)
                        ? (bool)expectCompilerExceptionProperty.GetValue(a)
                        : false;
                    if (expectCompilerException)
                    {
                        options.Add(DoNotEagerCompile);
                    }

                    return new BurstCompileAttribute(FloatPrecision.Standard, FloatMode.Default)
                    {
                        CompileSynchronously = true,
                        Options = options.ToArray(),
                    };
                }
            }

            return null;
        }

        internal static bool HasBurstCompileAttribute(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            BurstCompileAttribute attr;
            return TryGetAttribute(member, out attr);
        }

        /// <summary>
        /// Merges the attributes from the assembly into the member attribute, such that if any field of the member attribute
        /// was not specifically set by the user (or is a default), the assembly level setting is used for the Burst compilation.
        /// </summary>
        internal static void MergeAttributes(ref BurstCompileAttribute memberAttribute, in BurstCompileAttribute assemblyAttribute)
        {
            if (memberAttribute.FloatMode == FloatMode.Default)
            {
                memberAttribute.FloatMode = assemblyAttribute.FloatMode;
            }

            if (memberAttribute.FloatPrecision == FloatPrecision.Standard)
            {
                memberAttribute.FloatPrecision = assemblyAttribute.FloatPrecision;
            }

            if (memberAttribute.OptimizeFor == OptimizeFor.Default)
            {
                memberAttribute.OptimizeFor = assemblyAttribute.OptimizeFor;
            }

            if (!memberAttribute._compileSynchronously.HasValue && assemblyAttribute._compileSynchronously.HasValue)
            {
                memberAttribute._compileSynchronously = assemblyAttribute._compileSynchronously;
            }

            if (!memberAttribute._debug.HasValue && assemblyAttribute._debug.HasValue)
            {
                memberAttribute._debug = assemblyAttribute._debug;
            }

            if (!memberAttribute._disableDirectCall.HasValue && assemblyAttribute._disableDirectCall.HasValue)
            {
                memberAttribute._disableDirectCall = assemblyAttribute._disableDirectCall;
            }

            if (!memberAttribute._disableSafetyChecks.HasValue && assemblyAttribute._disableSafetyChecks.HasValue)
            {
                memberAttribute._disableSafetyChecks = assemblyAttribute._disableSafetyChecks;
            }
        }

        /// <summary>
        /// Gets the options for the specified member. Returns <c>false</c> if the `[BurstCompile]` attribute was not found.
        /// </summary>
        /// <returns><c>false</c> if the `[BurstCompile]` attribute was not found; otherwise <c>true</c></returns>
        internal bool TryGetOptions(MemberInfo member, bool isJit, out string flagsOut, bool isForEagerCompilation = false, bool isForILPostProcessing = false)
        {
            flagsOut = null;
            if (!TryGetAttribute(member, out var memberAttribute, isForEagerCompilation))
            {
                return false;
            }

            if (TryGetAttribute(member.Module.Assembly, out var assemblyAttribute))
            {
                MergeAttributes(ref memberAttribute, in assemblyAttribute);
            }

            flagsOut = GetOptions(isJit, memberAttribute, isForEagerCompilation, isForILPostProcessing);
            return true;
        }

        internal string GetOptions(bool isJit, BurstCompileAttribute attr = null, bool isForEagerCompilation = false, bool isForILPostProcessing = false)
        {
            // Add debug to Jit options instead of passing it here
            // attr.Debug

            var flagsBuilderOut = new StringBuilder();

            if (isJit && !isForEagerCompilation && ((attr?.CompileSynchronously ?? false) || RequiresSynchronousCompilation))
            {
                AddOption(flagsBuilderOut, GetOption(OptionJitEnableSynchronousCompilation));
            }

            if (isForILPostProcessing)
            {
                // IL Post Processing compiles are the only thing set to low priority.
                AddOption(flagsBuilderOut, GetOption(OptionJitCompilationPriority, CompilationPriority.ILPP));
            }
            else if (isJit && isForEagerCompilation)
            {
                // Eager compilation must always be asynchronous.
                // - For synchronous jobs, we set the compilation priority to HighPriority.
                //   This has two effects:
                //   - These synchronous jobs will be compiled before asynchronous jobs.
                //   - We will block on these compilations when entering PlayMode.
                // - For asynchronous jobs, we set the compilation priority to LowestPriority.
                //   These jobs will be compiled after "normal" compilation requests
                //   for asynchronous jobs, and crucially after all LowPriority ILPostProcessing
                //   jobs (which can map to the same function pointer to-be-compiled underneath
                //   and cause compilation thrads to stall).
                // Note that we ignore the global "compile synchronously" option here because:
                // - If it's set when entering play mode, then we'll wait for all
                //   methods to be compiled anyway.
                // - If it's not set when entering play mode, then we only want to wait
                //   for methods that explicitly have CompileSynchronously=true on their attributes.
                var priority = (attr?.CompileSynchronously ?? false)
                    ? CompilationPriority.EagerCompilationSynchronous
                    : CompilationPriority.EagerCompilationAsynchronous;
                AddOption(flagsBuilderOut, GetOption(OptionJitCompilationPriority, priority));

                // Don't call `burst.initialize` when we're eager-compiling.
                AddOption(flagsBuilderOut, GetOption(OptionJitSkipBurstInitialize));
            }

            if (attr != null)
            {
                if (attr.FloatMode != FloatMode.Default)
                {
                    AddOption(flagsBuilderOut, GetOption(OptionFloatMode, attr.FloatMode));
                }

                if (attr.FloatPrecision != FloatPrecision.Standard)
                {
                    AddOption(flagsBuilderOut, GetOption(OptionFloatPrecision, attr.FloatPrecision));
                }

                // We disable safety checks for jobs with `[BurstCompile(DisableSafetyChecks = true)]`.
                if (attr.DisableSafetyChecks)
                {
                    AddOption(flagsBuilderOut, GetOption(OptionDisableSafetyChecks));
                }

                if (attr.Options != null)
                {
                    foreach (var option in attr.Options)
                    {
                        if (!string.IsNullOrEmpty(option))
                        {
                            AddOption(flagsBuilderOut, option);
                        }
                    }
                }

                switch (attr.OptimizeFor)
                {
                    case OptimizeFor.Default:
                    case OptimizeFor.Balanced:
                        AddOption(flagsBuilderOut, GetOption(OptionOptLevel, 2));
                        break;
                    case OptimizeFor.Performance:
                        AddOption(flagsBuilderOut, GetOption(OptionOptLevel, 3));
                        break;
                    case OptimizeFor.Size:
                        AddOption(flagsBuilderOut, GetOption(OptionOptForSize));
                        AddOption(flagsBuilderOut, GetOption(OptionOptLevel, 3));
                        break;
                    case OptimizeFor.FastCompilation:
                        AddOption(flagsBuilderOut, GetOption(OptionOptLevel, 1));
                        break;
                }
            }

            if (ForceEnableBurstSafetyChecks)
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.ForceOn));
            }
            else if (EnableBurstSafetyChecks)
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.On));
            }
            else
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.Off));
            }

            if (isJit && EnableBurstTimings)
            {
                AddOption(flagsBuilderOut, GetOption(OptionJitLogTimings));
            }

            if (EnableBurstDebug || (attr?.Debug ?? false))
            {
                AddOption(flagsBuilderOut, GetOption(OptionDebugMode));
            }

            return flagsBuilderOut.ToString();
        }

        private static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append('\n'); // Use \n to separate options

            builder.Append(option);
        }
        internal static string GetOption(string optionName, object value = null)
        {
            if (optionName == null) throw new ArgumentNullException(nameof(optionName));
            return "--" + optionName + (value ?? String.Empty);
        }

        private void OnOptionsChanged()
        {
            OptionsChanged?.Invoke();
        }

        private void MaybeTriggerRecompilation()
        {
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
            if (IsGlobal && IsEnabled && !IsInitializing)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Burst", "Waiting for compilation to finish", -1);
                try
                {
                    BurstCompiler.TriggerRecompilation();
                }
                finally
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                }
            }
#endif
        }

        /// <summary>
        /// This method should be called before changing any option that requires
        /// an Editor restart in versions older than 2019.3.
        ///
        /// This is because Editors older than 2019.3 don't support recompilation
        /// of already-compiled jobs.
        /// </summary>
        private void MaybePreventChangingOption()
        {
#if UNITY_EDITOR && !UNITY_2019_3_OR_NEWER
            if (IsGlobal && !IsInitializing)
            {
                if (RequiresRestartUtility.CalledFromUI)
                {
                    RequiresRestartUtility.RequiresRestart = true;
                }
                else
                {
                    throw new InvalidOperationException("This option cannot be set programmatically in 2019.2 and older versions of the Editor");
                }
            }
#endif
        }

#if !UNITY_DOTSPLAYER && !NET_DOTS
        /// <summary>
        /// Static initializer based on command line arguments
        /// </summary>
        static BurstCompilerOptions()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                switch (arg)
                {
                    case DisableCompilationArg:
                        ForceDisableBurstCompilation = true;
                        break;
                    case ForceSynchronousCompilationArg:
                        ForceBurstCompilationSynchronously = true;
                        break;
                }
            }

            if (CheckIsSecondaryUnityProcess())
            {
                ForceDisableBurstCompilation = true;
                IsSecondaryUnityProcess = true;
            }
        }

        private static bool CheckIsSecondaryUnityProcess()
        {
#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary
                || UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return true;
            }
#elif UNITY_2020_2_OR_NEWER
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Slave
                || UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return true;
            }
#elif UNITY_2019_4_OR_NEWER
            if (Unity.MPE.ProcessService.level == Unity.MPE.ProcessLevel.UMP_SLAVE
                || UnityEditor.Experimental.AssetDatabaseExperimental.IsAssetImportWorkerProcess())
            {
                return true;
            }
#endif
#endif

            return false;
        }
#endif
#endif // !BURST_COMPILER_SHARED
    }

#if UNITY_EDITOR
    // NOTE: This must be synchronized with Backend.TargetPlatform
    internal enum TargetPlatform
    {
        Windows = 0,
        macOS = 1,
        Linux = 2,
        Android = 3,
        iOS = 4,
        PS4 = 5,
        XboxOne = 6,
        WASM = 7,
        UWP = 8,
        Lumin = 9,
        Switch = 10,
        Stadia = 11,
        tvOS = 12,
        EmbeddedLinux = 13,
        GameCoreXboxOne = 14,
        GameCoreXboxSeries = 15,
        PS5 = 16,
    }
#endif

    // Need this enum for CPU intrinsics to work, so exposing it to Tiny too
#endif //!UNITY_DOTSPLAYER && !NET_DOTS
    // Don't expose the enum in Burst.Compiler.IL, need only in Unity.Burst.dll which is referenced by Burst.Compiler.IL.Tests
#if !BURST_COMPILER_SHARED
// Make the enum public for btests via Unity.Burst.dll; leave it internal in the package
#if BURST_INTERNAL
    public
#else
    internal
#endif
    // NOTE: This must be synchronized with Backend.TargetCpu
    enum BurstTargetCpu
    {
        Auto = 0,
        X86_SSE2 = 1,
        X86_SSE4 = 2,
        X64_SSE2 = 3,
        X64_SSE4 = 4,
        AVX = 5,
        AVX2 = 6,
        WASM32 = 7,
        ARMV7A_NEON32 = 8,
        ARMV8A_AARCH64 = 9,
        THUMB2_NEON32 = 10,
        ARMV8A_AARCH64_HALFFP = 11,
    }
#endif
#if !UNITY_DOTSPLAYER && !NET_DOTS

    /// <summary>
    /// Flags used by <see cref="NativeCompiler.CompileMethod"/> to dump intermediate compiler results.
    /// </summary>
    [Flags]
#if BURST_COMPILER_SHARED
    public enum NativeDumpFlags
#else
    internal enum NativeDumpFlags
#endif
    {
        /// <summary>
        /// Nothing is selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Dumps the IL of the method being compiled
        /// </summary>
        IL = 1 << 0,

        /// <summary>
        /// Dumps the reformated backend API Calls
        /// </summary>
        Backend = 1 << 1,

        /// <summary>
        /// Dumps the generated module without optimizations
        /// </summary>
        IR = 1 << 2,

        /// <summary>
        /// Dumps the generated backend code after optimizations (if enabled)
        /// </summary>
        IROptimized = 1 << 3,

        /// <summary>
        /// Dumps the generated ASM code
        /// </summary>
        Asm = 1 << 4,

        /// <summary>
        /// Generate the native code
        /// </summary>
        Function = 1 << 5,

        /// <summary>
        /// Dumps the result of analysis
        /// </summary>
        Analysis = 1 << 6,

        /// <summary>
        /// Dumps the diagnostics from optimisation
        /// </summary>
        IRPassAnalysis = 1 << 7,

        /// <summary>
        /// Dumps the IL before all transformation of the method being compiled
        /// </summary>
        ILPre = 1 << 8,

        /// <summary>
        /// Dumps all normal output.
        /// </summary>
        All = IL | ILPre | IR | IROptimized | Asm | Function | Analysis | IRPassAnalysis
    }

#if BURST_COMPILER_SHARED
    public enum CompilationPriority
#else
    internal enum CompilationPriority
#endif
    {
        EagerCompilationSynchronous  = 0,
        Asynchronous                 = 1,
        ILPP                         = 2,
        EagerCompilationAsynchronous = 3,
    }

#if UNITY_EDITOR
    /// <summary>
    /// Some options cannot be applied until after an Editor restart, in Editor versions prior to 2019.3.
    /// This class assists with allowing the relevant settings to be changed via the menu,
    /// followed by displaying a message to the user to say a restart is necessary.
    /// </summary>
    internal static class RequiresRestartUtility
    {
        [ThreadStatic]
        public static bool CalledFromUI;

        [ThreadStatic]
        public static bool RequiresRestart;
    }
#endif

    internal readonly struct EagerCompilationRequest
    {
        public EagerCompilationRequest(string encodedMethod, string options)
        {
            EncodedMethod = encodedMethod;
            Options = options;
        }

        public readonly string EncodedMethod;
        public readonly string Options;
    }
#endif //!UNITY_DOTSPLAYER && !NET_DOTS
}
