# Changelog

## [1.6.6] - 2022-06-02


### Fixed
- Fixed hashing error that could occur with unbound generic type
- Fixed a memory leak where during hashing we'd pin a GC object and never unpin and free it.
- Fixed a bug where using `ReinterpretStore(someIndex, (ushort)someValue)` could cause an internal compiler error in Burst.
- Fixed bug in static constructor ordering in the presence of indirect dependencies between static constructors (i.e. static constructor -> static method -> static constructor) that could result in a runtime crash
- Fixed a bug that manifested when using IAP in UWP builds with Burst - we were scanning the assembly hierarchy too deep for Bursted types, which led to an `AssemblyResolutionException`.
- Added workaround for "cannot dlopen until fork() handlers have completed" issue seen in macOS 12.3
- Fixed compiler crash when trying to dynamically call `BurstCompiler.CompileFunctionPointer`  in Burst-compiled code
- Fix compiler crash when the only usage of a static field was in a formatted exception string
- Fixed a bug with `Span` and `ReadOnlySpan` types where if the indices used were not already 32-bit signed integers, an internal compiler error would occur if running with safety checks enabled.
- A potential issue with the debug info mover pass, that meant it only affected the first entry point in a module
- Fixed hashing bug that could occur when a function pointer type is used in a method parameter
- Fixed hashing error that could occur in the presence of multiple synthesized explicit interface implementations with the same name and signature
- Fixed a compiler crash if users used `__refvalue` or `__arglist` in Burst. Neither of these are supported, but now we will nicely tell you via a compiler error that they aren't supported.
- Fixed a compiler error when trying to acquire the function pointer of a generic function from Bursted code.
- Using a function only through a C# function pointer could cause a crash

### Added
- Support for __Unmanaged for non-extern functions

### Removed

### Changed
- Used explicit namespace for UnityEditor.PackageManager.Events to avoid conflicts.

### Known Issues

## [1.6.5] - 2022-03-08


### Fixed
- Improved UWP linker error message to clarify which VS components need to be installed for UWP
- Fixed a bug that meant Burst was accidentally enabled in secondary Unity processes, including the asset import worker and out-of-process profiler (see changelog entry for 1.6.0-pre.1 for more context around this)
- Fixed access violation error that could occur when reading from a static readonly variable
- Made `--burst-force-sync-compilation` command-line option actually work
- Fix a bug where codegen differences could occur when using a local vector variable that was being captured by reference and passed to a called function, versus when it wasn't.
- Fixed an exception that could occur if you had the Burst AOT Settings menu docked in the Editor, and then did a player build. Trying to change any of the Burst AOT Settings would throw an exception (unless you closed and reopened the Burst AOT Settings).
- Fix the Burst `link.xml` output to preserve C# methods we rely on, alongside the static constructors that we preserved previously.
- Fixed errors when working with paths containing special characters
- Fixed a bug where if you had two generic structs overlapping at the same `FieldOffset` in an explicitly laid out struct, a codegen error would occur.
- Fixed a bug where if you used `FloatMode.Fast` with `math.pow`, where the `y` argument to `math.pow` was actually sourced from an integer, illegal codegen would be generated (LLVM would try and call out to `powf` from the cstdlib).
- Fixed another rare case of the file-is-locked bug where the Burst IL Post Processor could incorrectly hold a file lock on a pdb.
- Fixed a bug when calling profiling `CreateMarker` on iOS, Burst could fail at runtime saying it was unable to find `CreateMarker__Unmanaged`.

## [1.6.4] - 2022-01-10


### Fixed
- Fixed some memory leaks between the C# and C++ parts of the Burst compiler, and added some CI tooling to ensure this doesn't happen again.
- Fixed editor crash when trying to debug a DirectCalled method
- Error if install in build folder is used without ever using a regular build.
- Fixed a compiler miscompile if you loaded a `static readonly v128` and passed it straight to a function as an argument.
- Fixed a Unity 2021.2 and newer bug that manifested with UWP builds - we were using the wrong `unityaot` folder in the Unity editor distribution with Burst.
- Fixed a null-derefence that could occur if you used `UnsafeUtility.IsUnmanaged` in a `static readonly` constructor.
- Fixed a bug where the compiler would reject a `try`/`finally` statement if it was the first thing in a method
- Fix for the `X.pdb: The process cannot access the file because it is being used by another process` issue our users were seeing. We were taking a `FileShare.Read` lock, when we needed to take `FileShare.ReadWrite`.

### Added

### Removed

### Changed

### Known Issues

## [1.6.3] - 2021-11-19


### Fixed
- Fixed a bug where `fixed` used in conjunction with `Span` or `ReadOnlySpan` would cause a compiler error.
- Fixed a codegen issue with Unity 2021.2 and `System.Buffer.MemoryCopy`.
- Fix error when trying to Direct Call a method belonging to a private nested type
- Fixed an issue where with optimizations disabled, using half conversions on platforms that did not natively support half could cause linker errors.

## [1.6.2] - 2021-11-10

### Fixed
- Fixed type initialization error, and invalid log messages about needing to add `[MonoPInvokeCallback]` to be compatible with IL2CPP, that could occur in a player build with Burst disabled
- ILPP issue for dots runtime whereby a calli patch could generate bad IL if the first instruction replaced was the target of a branch.
- Fixed "UnityException: CompileAsyncDelegateMethod can only be called from the main thread." error that was logged in standalone players when the first invocation of a direct-call method was from a background thread
- Fixed constant folding when using `Hint.Likely` or `Hint.Unlikely` intrinsics - the compiler is now able to fold these calls away entirely if the input value is constant.
- Fixed a bug that made exiting play mode slower than it should have been when Synchronous Compilation was turned off
- Fixed an internal compiler error when casting a void* to a pointer-to-vector and then access the element.

### Added
- Burst now generates a link.xml automatically to avoid issues with stripping causing missing symbols at runtime from static constructor usage.

### Removed

### Changed
- Burst now waits for all threads to complete on shutdown, rather than performing a thread abort, as that could lead to a race condition with Dispose.
- Improved the compiler performance when doing large struct copies by detecting more cases where a load/store can be safely converted to a move-memory operation.

### Known Issues

## [1.6.1] - 2021-10-12


### Added
- Added support for `System.Span<T>` and `System.ReadOnlySpan<T>` within Bursted code. These types are not allowed as entry-point arguments.

### Removed
- Removed the player build `BC1370` exception warnings as users only found them annoying.

### Changed
- External methods can now include a special suffix to opt-in to being valid along with certain hard-coded ones

### Fixed
- The cache for pdbs was becoming stale. This caused issues with wrong source information being shown in the inspector, and potentially wrong debug information being generated for bursted code in editor sessions.
- Fixed a bug with `sqrt_ps` for 128-bit types where it would crash the compiler.
- Fixed a bug where a player build that had multiple assemblies that had structs declared with the same name and same contents but different `[BurstCompile]` methods in them, would wrongly only pick a single struct to Burst-compile.
- Crash in burst module initialization if multiple modules are compiled and then linked in a different order.
- Fixed our platform documentation to accurately reflect the current supported platforms with Burst.
- Static constructor sorting didn't account for dependencies within calls' IL
- Static constructor cyclic checks also included method calls when this is not necessary and fails on burst runtime logging code
- Burst will now handle projects special characters in their project-name
- Fix the `ITMS-90562: Invalid Bundle` submissions issue that users were experiencing when submitting Apps to the Apple App Store for iOS/tvOS.
- Fixed that implicitly casting a scalar half to a vector type would cause the compiler to crash.
- Stack overflow caused by placement of alloca under certain function transforms.
- Fixed a crash that could occur when loading legacy Burst AOT settings and then entering play mode
- Fixed issue that could cause bcl.exe to fail with an exit code of 1 but not output any compilation errors

### Known Issues
- Burst does not work correctly when a project has a semi-colon in its name

## [1.6.0] - 2021-08-26

### Fixed
- Fixed a bug whereby sometimes some LLVM intrinsics could be incorrectly marked as unused causing invalid codegen with calls to `math.acos`.

## [1.6.0-pre.4] - 2021-08-18


### Added
- Added support for DOTS Runtime running / loading .Net Core assemblies.

### Removed

### Changed

### Fixed
- Fixed an issue where dsym folders would be not be copied across to the DoNotShip folder when building a multi architecture build for mac os.
- Fixed bug that could lead to "Failed to resolve method with name hash X and signature hash Y" compiler error
- Fixed compiler error that occurred when calling `BurstCompiler.CompileFunctionPointer` with a delegate type that was decorated with a custom attribute
- Fixed the "could not find path tempburstlibs" error message popping up when building for Android and Burst is disabled
- Fixed bug that could lead to incorrect compiler errors for calls to `GetHashCode` from a generic type
- Incorrect conversions between signed and unsigned vector types
- Detects if the simulator is the target of a player build for iOS/tvOS and disables burst, as at present this configuration is not supported by burst.
- `[SkipLocalsInit]` now correctly doesn't zero-initialize variables in a function (previously it only avoided zero-initialization of `stackalloc` created variables).

### Known Issues

## [1.6.0-pre.3] - 2021-07-27


### Fixed
- Fixed a bug where methods with the same name and namespace, but in different assemblies, could resolve to the wrong method.
- Burst no longer logs a warning when opening the standalone Profiler
- Fixed an `UnauthorizedAccessException` that could occur when using Burst in players built for the macOS App Sandbox
- Fixed a bug that could cause an incorrect compilation error when using a primitive type as a generic argument in a static method entry point
- Crash due to member function debug information on tvOS.
- Fix documentation to make clear that `ref` / `out` parameters are supported on `[BurstDiscard]` methods.
- Fixed a `NullReferenceException` in the Burst compiler when multi-dimensional arrays were used. The compiler now produces a correct error message telling users that multi-dimensional arrays are not supported by Burst.
- Fixed DOTS Runtime Job Marshalling behaviour to properly handle marshalling generic Job types when not all closed forms of the generic type require marshalling.
- Fixed a Burst package warning in our editor compiler integration with respect to `BuildOptions.EnableHeadlessMode`.
- Fixed small race which could cause an unexpected exception when finishing a standalone compilation task.
- Building for Apple Silicon architecture on macOS would produce a universal binary, now it behaves correctly.
- tvOS/iOS and other statically linked platforms would fail to burst compile if the burst compiled code contained references to functions that were `[DllImport("__Internal")]`, due to a mismatch in calling convention.
- Fixed a bug whereby if you had `$"{too} {many} {fixed} {string} {formatted} {arguments}"` in a string formatter, Burst wouldn't be able to correctly understand how to transform this for the purposes of logging or fixed-string construction.
- Fixed where `Unity.Burst.CompilerServices.Constant.IsConstantExpression` is evaluated to be later in the compilation pipeline, to let it catch more constant expressions (for instance post-inlining).
- Rare non zero return code from bcl after successfully building..
- Only check assembly cache when the main-thread is requesting some Burst code - meaning that kicking off eager compilation is 1.6x faster than before.
- `stackalloc byte[]` with an array initializer was previously only supported when the `stackalloc` size was 8 or less. Sizes greater than 8 are now supported.
- Fixed an error that could occur with the form "System.InvalidOperationException: Could not find `burst.initialize` function in library 'SomeLibrary'"
- Fixed incorrect runtime behavior that could occur when casting a pointer to a generic type
- Fixed a bug where stackalloc's could be wrongly hoisted out of loops.
- Added [Preserve] attribute to prevent stripping a compiler service call
- Fixed incorrect compiler error that could occur when casting a pointer to a generic type and then calling a method with generic parameters
- Fixed incorrect compiler error that could occur with explicit-layout structs when setting a `Size` smaller than the natural struct size

### Added
- Universal (Apple Silicon + X64) versions of extra build tools
- Add Android x86_64 and re-enable x86 support
- Added support for having `[MarshalAs(UnmanagedType.U1)]` or `[MarshalAs(UnmanagedType.I1)]` on a `bool` external function parameter.
- Neon intrinsics: Added vst1* experimental APIs
- Added a global player build setting to let users specify the default optimization choice for Burst.
- Native support for Apple Silicon.
- Added support for `StructLayoutAttribute.Pack`
- Additional notes about `BurstCompiler.CompileFunctionPointer<T>` regarding; avoid wrapping in another open generic method, and interoperability with IL2CPP.

### Removed
- Removed the `Enable Safety Checks` option for player builds, since it didn't actually enable safety checks in containers, which are editor only in Unity.

### Changed
- Changed how we link object files for iOS and tvOS platforms such that Burst will now create the object file and hand it off to XCode for linking only.
- Assembly-level attributes (such as `[assembly: RegisterGenericJobType]`) are now scanned for generic job types to compile
- Fixed a regression that caused eager-compilation at Editor startup to be slower than it should have been
- `math.f16tof32` now uses hardware intrinsics where available (AVX2 / NEON).
- `half` to `float` or `double` vector conversions now produce more optimal codegen.
- Burst Inspector now remembers scroll position between domain reloads
- Changed how we schedule Burst eager compilation threads. Previously we'd spawn at most 8 of the threads, and only allow 2 to make progress while in the Editor (to ensure the editor UX/UI was as responsive as possible). Instead we now spawn `number_of_cores - 1` threads at a lower thread priority, ensuring that any computing power slack can be consumed to speed up Burst compilation. On a 24 core machine this resulted in 2.5x reduction in time taken for Burst to fully compile a large project.
- Fixed a potential error related to duplicate symbols when calling `BurstCompiler.CompileFunctionPointer` from inside Burst code
- Improved performance of checking the cache to see if methods have already been compiled
- For player builds : lib_burst_generated.txt, pdbs (in non development mode) and dysm folders are now placed into a xxx_BurstDebugInformation_DoNotShip folder alongside the data folder, this is to ensure it is easy to remove the files that you should not ship with your player.

### Known Issues
- Code that previously mixed managed or non-`readonly` static fields with Burst compiled code will now fail to compile.

## [1.6.0-pre.2] - 2021-04-15


### Fixed
- Fixed obsolete API in package code.


## [1.6.0-pre.1] - 2021-04-14


### Changed
- Start 1.6 release cycle
- Changed how we resolve function references in the compiler to improve resolving an existing function reference by 3x.
- Improve how we handle generic resolution in Cecil to cache the strictly resolved generic types and save a bunch of time in the compiler.
- Exception strings no longer contain the entry-point name of the job/function-pointer that caused the throw. This change was required because the Burst compiler has to produce deterministic results from any given compile, which is fundamentally opposed to per-entry-point function derivations.
- Changed how SLEEF global variables for trig functions are pulled into Burst to reduce duplications.
- Changed how exceptions throw types and messages are stored in our Burst binaries to reduce binary size.
- Constant array data is now named after the static field it belongs to in assembly
- Upgraded Burst to use LLVM Version 11.0.1 by default, bringing the latest optimization improvements from the LLVM project.
- The `Unity.Burst.Intrinsics.Common.Pause` intrinsic is no longer experimental.
- DOTS Runtime shares the logging code path with the general case
- Armv8.2 Neon intrinsics are now fully supported
- Disable threading within the `lld` linker instances we use for in-editor and desktop cross compilation, because we're already threading seperate process instances of `lld` and it results in lot of OS context switching.
- Tweaked how the IL Post Processed 'direct call' Burst function pointers are compiled so that the compilation is deferred until they are needed (previously we'd enqueue them all for compilation on a domain reload).
- Changed Burst minimum editor version to 2019.4
- Use rpmalloc as our native allocator on Windows to speed up concurrently executing LLVM work.
- When Burst has previously compiled a method, and neither the assembly containing that method nor any of that assembly's dependencies have changed, it was possible after a domain reload for the Mono version of the method to be used for a short time before being replaced by the Burst version. This has now been improved such that the Burst version will be used immediately.
- Improved iteration speed by reducing the time it takes for Burst to check if any Burst-compilable code has changed
- Change our link step to not use response files if the command line was smaller enough, saving the cost of the round-trip to the disk.
- Made half <-> float / double conversions use native hardware where possible (Arm or AVX2 targets).
- In order to prevent conflicts with the main Unity process, Burst is now inactive in secondary Unity processes, including the asset import worker and out-of-process profiler. This means that in those secondary processes, code that would normally be Burst-compiled will now run under Mono. In a future release of Burst, we hope to lift this restriction and allow Burst-compiled code to run in secondary Unity processes.

### Fixed
- Fixed a bug in LLVM that it would incorrectly convert some memset -> memcpy if both pointers derived from the same memory address, and where one indexed into the 0th element of the pointer.
- Fixed namespace issue triggering a warning in the editor.
- Made `math.shuffle` compile correctly when non-constant `ShuffleComponent`'s are used.
- Fixed alignment issues associated with xxHash3 on ArmV7 (case 1288992)
- Fixed managed implementation of sub_ss intrinsic
- Fixed a bug that occurred when an explicitly laid out struct was used by a `dup` instruction, which caused an internal compiler error.
- Fixes DOTS Runtime JobProducer Bursting code to support JobProducers with multiple generic arguments, complex job wrapper and generic jobs.
- Fixed a bug where if a user had defined multiple implicit or explicit casts, the compiler could resolve to the wrong cast.
- Fixed a bug where explicitly casting from an int to `IntPtr` would not sign extend the value.
- String interpolation issues when using Dots / Tiny runtime.
- Fixed managed implementations of blend_epi32 and mm256_blend_epi32 intrinsics on Mono
- Fixed a bug where loading from a vector within a struct, that was got from a `NativeArray` using an indexer, would cause the compiler to crash.
- Fixed an issue where Burst would erroneously error on `BurstCompile.CompileFunctionPointer ` calls when building for the DOTS Runtime.
- clang segmentation fault on iOS when member function debug information was emitted, it is disabled for this platform now.
- Intrinsics: Neon - fixed vget_low and vget_high producing suboptimal code
- Private `[BurstCompile]` methods no longer throw `MethodAccessException`
- Fixed a bug where the Burst post-processing for direct call would cause duplicate function pointers to be compiled, wasting compile time in the editor and caused an Editor launch stall.
- Corrected 'Enable safety checks tooltip`.
- Fixed a minor debug information bug where built-in types with methods (like `System.Int32`) would generate incorrect debug information.
- Fixed a very obscure bug where if you had a function-pointer that was called from another function-pointer of job, and that function-pointer happened to be compiled in a player build in the same bucket as the caller, and the no-alias cloning analysis identified that it could clone the original function-pointer to enable more aliasing optimizations, it could create a duplicate symbol error.
- Revert to internal linkage for Android X86 (32bit) to ensure ABI compliance.
- Fixed compilation errors when targeting Arm CPUs and using some of the Intel intrinsics
- Added PreserveAttribute to prevent the internal log from being stripped in il2cpp builds.
- IL Function Pointer Invoke Transformation updated to handle transforms that affect instructions that are the destination of a branch.
- IL Function Pointer Invoke Transformation now uses correct runtime library for dots runtime.
- Fixed compilation errors when targeting Intel CPUs and using some of the Arm Neon intrinsics
- Fixed a bug where eager-compilation could pick up out-of-date global Burst menu options for compiling.
- Fixed a bug where the progress bar would report double the amount of pending compile jobs if a user changed the Burst options while background compilation was going on.
- Fixed some intrinsics not checking target CPU against required CPU, so it was possible to use some intrinsics without an IsXXXSupported check
- Fixed a bug where having any `[DllImport]` in a class that used the Direct Call mechanism could result in an illegal `CompileFunctionPointer` call being produced by our post processor.
- Fixed an issue where if a user used a math function (like `cos`, `sin`, etc) then LLVM would preserve both the scalar and vector implementations even if they were trivially dead, causing us to inject otherwise dead functions into the resulting binary.
- PDB debug information for instance methods that also used struct return were incorrect.
- When generating Line Table only debug information, an unreachable could occur due to a missing check.
- Fixed the 1.5 restriction that Direct Call methods can only be called from the main thread, now they work when called from any thread.
- Internal Compiler Error if a call was discarded (via BurstDiscard for example), but the callsites required an ABI transform e.g. struct return.
- Fixed a bug with using multiple `IsXXXSupported` intrinsics in the same boolean condition would fail.
- Broken link restored for known issues with debugging and profiling.
- The Direct Call injected delegate now has a unique suffix to avoid type-name clashes.
- Dots runtime function pointer transform has been simplified, making it less brittle and fixing some bad IL generation.
- Fixed crashes on 32 bit windows when calling function pointers from managed code and using IL2CPP.
- Fixed a possible DivideByZeroException due to race condition in TermInfoDriver initialization code.
- Fixed a bug where the multi-CPU dispatcher (used for player builds targetting multiple CPU architectures) could end up generating invalid instructions.
- Gracefully handle failing to find a particular assembly in the ILPP to prevent an ICE.
- function calls using in modifiers on blittable structs where being treated as non blittable.
- crash when extracting sequence point information for error reporting/debug information generation.
- Direct Call extension methods that only differ on argument types are now supported (previously Burst's `AssemblyLoader` would complain about multiple matches).
- Fixed a regression where managed static fields, in static constructors that would also be compiled with Burst, could cause a compile time failure for mixing managed and unmanaged state.

### Added
- Added links to blog posts from the burst team to the Burst documentation.
- Intrinsics: Neon - Added support for basic vld1 APIs
- Can now call BurstCompiler.CompileFunctionPointer() in Burst code
- Add support for the C# 8.0 construct `default(T) is null` to Burst by transforming the generated `Box` + 'is the box non-null?' at compile time.
- Make it possible to get a pointer to UTF-8 encoded string literal data in HPC# code via StringLiteral.UTF8()
- Add an `OptimizeFor` option to `[BurstCompile]`, allowing users to say they want fast code, small code, or fastly compiled code.
- Known issue with Windows Native Debuggers and Dll numbers + workarounds.
- Assemblies are now allowed to have an `[assembly: BurstCompile()]` attribute to let users specify compile options that should apply assembly wide (for instance `[assembly: BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]`).
- Automatically add [UnmanagedFunctionPointer(CallingConvention.Cdecl)] to any delegates that are used for BurstCompiler.CompileFunctionPointer<>() or error if the delegate has the attribute and it is not Cdecl.
- Source location metadata into hash cache.
- Added support for having `[return: MarshalAs(UnmanagedType.U1)]` or `[return: MarshalAs(UnmanagedType.I1)]` on a `bool` return external function.
- An additional warning about delegates being used by `BurstCompiler.CompileFunctionPointer` that are not decorated as expected. In most cases, Burst will automatically add the C-declaration attribute in IL Post Processing, but if the usage of CompileFunctionPointer is abstracted away behind an open generic implementation, then Burst will not be able to automatically correct the delegate declaration, and thus this warning will fire.
- new `burst_TargetPlatform_EmbeddedLinux`
- new `AotNativeLinkEmbeddedLinux` for EmbeddedLinux
- Added a new `OptimizeFor` mode `Balanced`. This becomes the default optimization mode, and trades off slightly lower maximum performance for much faster compile times.
- Added experimental half precision floating point type f16
- Added experimental support for half precision floating point Arm Neon intrinsics

### Removed

### Known Issues
- Direct Call methods only execute using Burst after an initial execution of them on the main-thread.

### Notes
- BurstAotCompiler integration done using reflection and raw values, since the platform will only be officially available for 2021.2+ and we special customer versions (shadow branches) for 2019.4 & 2020.3.
- AotNativeLinkEmbeddedLinux implementation gets the toolchain from environment vars.

## [1.5.0-pre.2] - 2020-12-01

### Added

### Removed

### Changed

### Fixed
- Fixed a failure on linux builds where libdl.so cannot be found.

### Known Issues


## [1.5.0-pre.1] - 2020-11-26


### Added
- New intrinsics `Hint.Likely`, `Hint.Unlikely`, and `Hint.Assume` to let our users tell the compiler some additional information which could aid optimization.
- New `Bmi1` and `Bmi2` x86 intrinsics. These are gated on `AVX2` being supported to keep the feature sets that Burst has to support small.
- You can now select explicit x86/x64 architecture SIMD target for Universal Windows Platform.
- Added Apple silicon and macOS universal binaries support to Burst.
- An extra alloca hoisting step to ensure that allocas that occur deep within functions are correctly allocated in the function entry block (which LLVM requires for optimization purposes).
- Added the missing `clflush` intrinsic to the SSE2 intrinsics.
- An optimize-for-size option to bcl to let select users focus the optimization passes to create smaller executables.
- Added a `Unity.Burst.CompilerServices.SkipLocalsInitAttribute` attribute that lets developers tell the compiler that stack-allocations do not need to be zero initialized for a given function.
- Added a new attribute `[IgnoreWarnings]` that can be specified per method, for users that really want the compiler to be quiet.
- Support for RDMA, crypto, dotprod Armv8.2-A Neon intrinsics
- An error message if attempting to BurstCompiler.CompileFunctionPointer() on a multicast delegate, since this is not supported in Burst.
- Burst detects removal of the burst package in 2020.2 editors and beyond, and displays a dialog asking the user to restart the editor.
- Added a pass that will classify and remove dead loops for improved code generation.
- Add support for using `ValueTuple` types like `(int, float)` from within Burst code, as long as the types do not enter or escape the Burst function boundaries.
- Added a new intrinsic `Unity.Burst.CompilerServices.Constant.IsConstantExpression` that will return true if an expression is known to be a compile-time constant in Bursted code.
- Added support for PlayMode / Desktop Standalone Players to load additional burst compiled libraries for use in Modding.
- Add support for calling Burst code directly from C# without using function pointers.
- In Unity 2020.2 and above, you can now call `new ProfilerMarker("MarkerName")` from Burst code
- Add a compiler error if a `ldobj` tries to source its address to load from a non-pointer/non-reference. C# frontends should never generate this pattern, but we did see it with code generation.

### Fixed
- Fixed an issue where a function with a `[return: AssumeRange(13, 42)]` could lose this information during inlining.
- Storing into `Lo64` or `Hi64` would cause a compiler exception.
- Hitting a `ldobj` of a pointer-to-vector would incorrectly load the vector rather than the pointer.Burst only generates unaligned stores.
- Fix that the parameter to mm256_set1_epi8 should be a byte instead of a char.
- Fix sqrt_ss would fail because LLVM version later than 6 changed the encoding.
- Fixed the comi*_ss intrinsics which would generate invalid code.
- Pdb location for player builds is now linked relative to the final lib_burst_generated.dll, this allows the crashdump utility to access the symbols and provide better callstacks.
- Support negative intrinsics features checks to enable usage like `if (!IsSse41Supported) return;`.
- Clean up linker temp response files on successful build
- Wasm ABI issue with pointers
- Pause intrinsic in wasm (ignored)
- fmod expansion to sleef for wasm
- The AOT option for disabling optimizations now actually disables optimizations in player builds.
- Fix a bug where a `static readonly` variable that was a `System.Guid` would result in an internal compiler error.
- bitmask intrinsic was broken on non intel platforms
- When "Enable Compilation" was unchecked in the Burst menu, Burst was incorrectly enabled after an Editor restart. This is now fixed.
- Fixed a bug where a cloned function (say through no-aliasing propagation cloning) would re-create any global variables used rather than use the original variable.
- If the only reference to an external function was discarded, don't attempt to add it to the burst initialisation block (which caused on ICE on prior versions).
- Fixed a case where extracting a `FixedString4096` from a parent struct could cause very slow compile times.
- Fixed a poor error message when a generic unsupported type (like a class or an auto-layout struct) combined with an unsupported managed array (like `(int, float)[]`) wouldn't give the user any context on where the code went wrong.
- Fixed a bug where if you used an enum argument to a function to index into a fixed array, a codegen error would occur.
- If targeting multiple iOS architectures, produce a combined burst library containing all architectures, this fixes "New Build System" on xcode version 12.
- Static method parameters are now validated correctly during eager-compilation
- Fixed permissions error when running lipo tool to combine libraries.
- Fixed compiler error that could occur when calling a `[BurstDiscard]` method with an argument that is also used elsewhere in the method
- Fixed an issue that could prevent the Editor from shutting down
- Fixed an internal compiler error when nested managed static readonly arrays were used (produces a proper Burst error instead now).
- Fixed a bug whereby for platforms that require us to write intermediate LLVM bitcode files, UTF paths would be incorrectly handled.
- Correctly marked Neon intrinsics vmovn_high_* as ArmV7 and not ArmV8
- On windows, the pdb location for burst cached dll's now points to the correct path. Native debuggers attached to the Editor should now locate the symbols without requiring adding the Library/Burst/JitCache folder to the symbol search.
- Re-enabled `BC1370` exception warnings but only for player builds.
- Fixed a bug whereby if you had an assembly that was guarded by `UNITY_SERVER`, Burst would be unable to find the assembly when `Server Build` was ticked.
- When "Enable Compilation" was unchecked in the Burst menu, Burst was incorrectly enabled after an Editor restart. This is now _actually_ fixed.
- `static readonly` array with enum elements would cause the compiler to crash.
- Fixed managed (reference) implementation of mm256_cvttps_epi32 (case 1288563)
- Debug information for instance methods is now correctly scoped. This means instance variables can now be inspected correctly.

### Removed
- Removed support for XCode SDKs less than version 11.0.0.
- Removed support for platform SDKs that used the older LLVM 6 and 7 in the codebase to significantly simply our code and reduce the package size.

### Changed
- Minimum SDK version for iOS/tvOS increased to 13. See https://developer.apple.com/news/?id=03042020b for details.
- When using "Executable Only" build type on Universal Windows Platform, Burst will now only generate code for a single CPU architecture that you're building for.
- The inliner heuristics have been modified to inline less functions, but improve compile times and reduce executable size.
- The minimum XCode SDK required to compile for iOS/iPadOS/tvOS is now 11.0.0.
- We now copy the lib_burst_generated.pdb into the root of the player build (in addition to being alongside the lib_burst_generated.dll), this allows the unity crash handler to resolve the callstacks from burst code.
- Made Arm Neon intrinsics fully supported (removed the guarding define)
- Improved eager-compilation performance
- Improved Burst Inspector loading time
- Improved Burst initialization time
- If an argument to a BurstDiscard method is discarded, and that argument is a method call, then a warning is now generated to indicate the function call no longer happens.
- Changed how struct-return and indirect arguments use stack allocations to significantly reduce stack usage and improve performance in these cases.
- Improved the compilers ability to deduce dead memory operations (memcpy, memset, etc) to improve performance.
- Improved error message seen when scheduling Burst compilation during domain reload
- Open-generic static methods are not supported by Burst, but they were previously visible in Burst Inspector - they are now hidden
- In Burst Inspector, the "Safety Checks" checkbox now defaults to unchecked
- Burst Inspector no longer loses the search filter and "Safety Checks" option after domain reload
- Changed exception throws to allow more vectorization chances surrounding them.
- Upgraded Burst to use LLVM Version 11.0.0 by default, bringing the latest optimization improvements from the LLVM project.
- Eager-compilation is now cancelled when script compilation starts, to prevent spurious errors related to recompiled assemblies
- Strings can now be passed between methods within Burst code. Previously, string literals used for e.g. `Debug.Log` calls could only appear in the same method where they were used; now the string literal can be in one method, and passed to another method via a `string` parameter.
- Transitioning from burst disabled to burst enabled in editor, will perform a re-initialise of some internal state in use by Direct Call methods.
- Improved the performance of in-compiler hashing by 1.2x.
- Improved our hashing performance some more by re-using fixed-sized buffers in the compiler to improve eager-compilation / warm-cache costs by 1.25x.
- Improved compile time by ~37% on big projects by reworking some core compiler infrastructure.

### Known Issues
- In player builds, exceptions can report the wrong job that they were thrown from.

## [1.4.0-preview.4] - 2020-08-17


### Fixed
- Fixed a bug introduced in 1.4.0-preview.3 that prevented some `UnityEngine.Debug` methods (such as `DrawLine`) from being called
- Fixed compiler error when explicit-layout struct contains a field which is itself an empty struct
- Fixed a bug that if you used more than four arguments in a function _declared within another function_, and then implicitly captured a few variables, Burst would map the variables wrongly.

### Changed
- Bump com.unity.mathematics to 1.2.1 version

## [1.4.0-preview.3] - 2020-08-06


### Added
- VS 2017 support for platform that needs it.
- Added first batch of Arm Neon intrinsics. Currently, only ArmV8 (AArch64) targets are supported. The supported intrinsics include all ArmV7 and ArmV8 ones.

### Removed

### Changed
- In versions of Unity older than 2019.3, changing the following options in the Burst menu now requires the Editor to be restarted: Enable Compilation, Safety Checks, and Native Debug Mode Compilation. In versions of Unity older than 2019.3, previously-compiled methods will not be recompiled after changing those options, which could lead to undefined behavior where methods may or may not be compiled with the correct options. This change removes that possibility.
- Improved performance of "eager-compilation" (scheduling compilation immediately after assemblies are changed) by cancelling queued eager-compilation when entering play mode with Synchronous Compilation unchecked
- Improved performance of eager-compilation by not eager-compiling test assemblies
- Asserts that are currently discarded no longer discard arguments with potential side effects.

### Fixed
- We no longer attempt to replace the debug metadata multiple times for a given export.
- Fixed a subtle codegen bug that could occur when the target is an Arm or AArch64 CPU with vectors of 3 elements.
- Inspector slow down when scrolling/moving the window on large listings.
- Fixed a bug where a `stfld` into an element of a vector could deduce the wrong type for the underlying vector.
- Fixed a potential error when running the linker with a failure on lld command.
- If path to the package contained spaces, then native command execution could fail. This would manifiest as weird errors with 'lld' or 'vswhere' or other native tools.
- Fixed Debug.Log by re-enabling it when used in function pointers or jobs.
- Fixed errors when opening Inspector with a non-public Execute method on a job producer type

### Known Issues

## [1.4.0-preview.2] - 2020-07-01


### Added

### Removed

### Changed
- The Burst Inspector no longer uses JIT compilation. The code it shows is now compiled the same way as for editor / player usage.
- Warnings are hidden in the inspector view

### Fixed
- Fixed potential error that could occur when unloading cached libraries

### Known Issues

## [1.4.0-preview.1] - 2020-06-26


### Added
- Experimental support for tvOS
- Add intrinsics support for `AtomicSafetyHandle.NewStaticSafetyId<T>`
- A new option `[BurstCompile(DisableSafetyChecks = true)]` that allows per job or function-pointer disabling of safety checks. This allows users to have blessed code run fast always.
- Improve Editor experience by scheduling compilation immediately after assemblies are changed, instead of waiting until Play Mode is entered.
- Improved our aliasing detection to allow `DynamicBuffer` structs to be easily vectorizable.
- Added a compiler warning for any use of throwing an exception from a method **not guarded by** `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]`. Since exceptions in Burst are only supported in the editor, this provides a useful warning to users who may be relying on try/catch behaviors for control-flow which is not supported in final game builds.
- Burst compilation status is now displayed in the Background Tasks window in Unity 2020.1 and above (click the spinner in the bottom-right of the Editor to open this window).
- Upgraded Burst to use LLVM Version 10.0.0 by default, bringing the latest optimization improvements from the LLVM project.
- Add support for try/finally and using/foreach for IDisposable patterns.
- Add `BurstCompiler.IsEnabled` API.
- Add syntax colouring for LLVM IR and Optimized IR panels in the inspector

### Removed

### Changed
- Made the compiler better at constant-folding complex static readonly constructors.
- Bursted DOTS Runtime Jobs are now decorated with `[NativePInvokeCallback]` instead of `[MonoPInvokeCallback]` which could generate callback wrappers which could cause native code to inadvertently interop with the managed VM.
- The Burst menu-item `Safety Checks` has been changed to a modal choice of `Off`, `On`, and `Force On`. `Force On` will overwrite any user job or function-pointer with `DisableSafetyChecks = true`. To avoid users falling into the consistent trap of having `Safety Checks` set to `Off`, any reload of the Editor will issue a warning telling the user that `Safety Checks` have been reset to `On`.
- Use platform provided memory intrinsics for iOS, tvOS, WASM, and console platforms.
- Updated Cross Compilation Tooling To LLVM 10
- The command line option `--burst-disable-compilation` is now disabling entirely Burst, including the AppDomain.

### Fixed
- Fixed incorrect struct layout for certain configurations of explicit-layout structs with overlapping fields
- Fixes a caching issue where stale cached libraries may have been used if a project was copied to a different folder, or Unity was upgraded to a newer version
- Burst will now error if a `cpblk` was used to copy into a `[ReadOnly]` parameter or field.
- Fixed a bug where the mm256_cvtepi32_ps intrinsic would crash the compiler.
- Fixed a bug with constant expressions that could cause a compile-time hang.
- Debug symbols are now output when using the native toolchain on mac.
- Sleef fallback to scalar float for WASM.
- ABI struct ret/by val for trivial aggregates for WASM is now respected.
- Fixed a bug with float/double vector constructors of `Unity.Mathematics` that take half or half vector parameters.
- Debug information for anonymous structs could be created partially multiple times for the same type.
- Filter symbol warnings to prevent them reaching logs.
- Fixed an issue where UNITY_DOTSPLAYER builds not building for NET_DOTS would be unable to compile do to references to UnityEngine.
- Fixed handling of conversion from signed integer to pointer which caused issues as discovered by Zuntatos on the forums.
- Allow to call `[BurstCompile]` functions from other `[BurstCompile]` functions
- IntPtr.Size now correctly returns int32 size (rather than UInt64) - fixes an assert.
- Burst package has been upgraded popup could fire erroneously under shutdown conditions.
- Fixed an issue preventing player builds to succeed when burst compilation is disabled.
- Debug symbols for function names on some platforms are no longer hashes.
- Job Entry point symbols should now reflect the job name and type rather than a hash in callstacks/native profilers
- Job entry points without symbols now use the Execute location rather than pointing to unknown/unknown
- Dwarf symbols from multiple modules (e.g. multithreaded AOT compilation) now have correct compilation unit information.

### Known Issues
- Output of `Debug.Log` is temporarily disabled in Burst Function Pointers/Jobs to avoid a deadlock on a domain reload. A fix for the Unity editor is being developed.

## [1.3.0-preview.12] - 2020-05-05


### Fixed
- Fix an issue when changing the base type of an enum that would not trigger a new compilation and would keep code previously compiled, leading to potential memory corruptions or crashes.
- Fixed a subtle AArch64 ABI bug with struct-return's (structs that are returned via a pointer argument) that was found by our partners at Arm.
- Fix an issue that was preventing Debug.Log to be used from a Job in Unity 2020.1

### Changed
- JIT cache is now cleared when changing Burst version

## [1.3.0-preview.11] - 2020-04-30


### Fixed
- Fix potentially different hashes returned from `BurstRuntime.GetHashCode32/64` if called from different assemblies.
- Fixed an issue where Burst was misidentifying F16C supporting CPUs as AVX2.
- SDK level bumped for MacOS to ensure notarization requests are compatable.
- Fixed a typo `m256_cvtsi256_si32` -> `mm256_cvtsi256_si32` and `m256_cvtsi256_si64` -> `mm256_cvtsi256_si64`.
- The compiler is now generating a proper compiler error if a managed type used directly or indirectly with SharedStatic<T>.
- Fixed a bug where implicitly stack allocated variables (`var foo = new Foo();`) in Burst were not being zero initialized, so any field of the variable that was not initialized during construction would have undefined values.
- Fix potential race condition when accessing on-disk library cache
- Fixed a bug where Burst was sometimes producing invalid code for iOS 11.0.3+.

### Added
- Added support for `System.Threading.Volatile` methods `Read` and `Write`, and for the `System.Threading.Thread.MemoryBarrier` method.
- New FMA X86 intrinsics. These are gated on AVX2 support, as our AVX2 detection requires the AVX2, FMA, and F16C features.
- `UnsafeUtility.MemCmp` now maps to a Burst optimal memory comparison path that uses vectorization.

### Removed

### Changed

### Known Issues

## [1.3.0-preview.10] - 2020-04-21


### Fixed
- Fix negation of integer types smaller than 32 bits.
- Fixed a bug where optimizer generated calls to `ldexp` would be incorrectly deduced when deterministic floating-point was enabled.
- Swapped private linkage for internal linkage on functions, this fixes duplicate symbol issues on some targets.
- variable scopes should now encompass the whole scope.
- variables in parent scopes should now be present in locals windows.
- Native plugin location for windows has changed in 2019.3.9f1. If you are on an older version of 2019.3 you will need to upgrade for burst to work in windows standalone players.
- Added an error if `Assert.AreEqual` or `Assert.AreNotEqual` were called with different typed arguments.
- Fixed a bug where doing an explicit cast to a `Unity.Mathematics` vector type where the source was a scalar would fail to compile.
- Fix issue when converting large unsigned integer values to double or float.
- Fix an invalid value returned from a conditional where one type is an int32 and the other type would be a byte extended to an int32.
- Button layout of disassembly toolbar tweaked.
- Copy to clipboard now copies exactly what is shown in the inspector window (including enhancements and colours if shown)
- AVX2 now generates the correct AVX2 256-bit wide SLEEF functions instead of the FMA-optimized 128-bit variants.

### Added
- Anonymous types are now named in debug information.
- XCode/LLDB debugging of burst compiled code is now possible on macOS.
- Added some extra documentation about how to enable `AVX`/`AVX2` in AOT builds, and how we gate some functionality on multiple instruction sets to reduce the combinations exposed underneath.
- Optimized external functions (like `UnsafeUtility.Malloc`) such that if they are called multiple times the function-pointer address is cached.
- Add support for string interpolation (e.g `$"This is a string with an {arg1} and {arg2}"`).
- Add support for Debug.Log(object) (e.g `Debug.Log("Hello Log!");`).
- Add support for string assignment to Unity.Collections.FixedString (e.g `"FixedString128 test = "Hello FixedString!"`).
- If burst detects a package update, it now prompts a restart of Unity (via dialog). The restart was always required, but could be missed/forgotten.
- Better error message for unsupported static readonly arrays.
- Link to native debugging video to Presentations section of docs.
- Fixes a bug where `in` parameters of interfaces could sometimes confuse the Burst inspector.

### Removed

### Changed
- iOS builds for latest xcode versions will now use LLVM version 9.
- Burst AOT Settings now lets you specify the exact targets you want to compile for - so you could create a player with SSE2, AVX, and AVX2 (EG. _without_ SSE4 support if you choose to).
- Improve speed of opening Burst Inspector by up to 2x.
- Provided a better error message when structs with static readonly fields were a mix of managed/unmanaged which Burst doesn't support.
- Tidied up the known issues section in the docs a little.
- Enhanced disassembly option has been expanded to allow better control of what is shown, and allow a reduction in the amount of debug metadata shown.
- Load Burst Inspector asynchronously to avoid locking-up Editor.
- Documented restrictions on argument and return types for DllImport, internal calls, and function pointers.

### Known Issues

## [1.3.0-preview.9] - 2020-04-01


### Changed
- Improved the compile time performance when doing `UnsafeUtility.ReadArrayElement` or `UnsafeUtility.WriteArrayElement` with large structs.
- Made some compile-time improvements when indirect arguments (those whose types are too big that they have to be passed by reference) that reduced our compile time by 3.61% on average.

### Fixed
- Fixed a bug where storing a `default` to a pointer that was generic would cause an LLVM verifier error.
- Fixed an obscure bug in how struct layouts that had dependencies on each other were resolved.
- Fixed a bug as found by [@iamarugin](https://forum.unity.com/members/iamarugin.737579/) where LLVM would introduce ldexp/ldexpf during optimizations that LLD would not be able to resolve.
- Fixed a bug where the compiler would not promote sub-integer types to integers when doing scalar-by-vector math (like multiplies).

### Added
- Variable scopes are now constructed for debug information.
- A new setting to Burst AOT Settings that allows debug symbols to be generated even in a non development standalone build.

### Removed

### Known Issues

## [1.3.0-preview.8] - 2020-03-24


### Added
- Double math builtins in `Unity.Mathematics` now use double vector implementations from SLEEF.
- Fixed a bug with `lzcnt`, `tzcnt`, and `countbits` which when called with `long` types could produce invalid codegen.
- New F16C X86 intrinsics. These are gated on AVX2 support, as our AVX2 detection requires the AVX2, FMA, and F16C features.
- Add user documentation about generic jobs and restrictions.
- Add new experimental compiler intrinsics `Loop.ExpectVectorized()` and `Loop.ExpectNotVectorized()` that let users express assumptions about loop vectorization, and have those assumptions validated at compile-time.Enabled with `UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS`.

### Changed
- Changed how `Unity.Mathematics` functions behave during loop vectorization and constant folding to substantially improve code generation.
- Our SSE4.2 support was implicitly dependent on the POPCNT extended instruction set, but this was not reflected in our CPU identification code. This is now fixed so that SSE4.2 is gated on SSE4.2 and POPCNT support.
- The popcnt intrinsics now live in their own static class `Unity.Burst.Intrinsics.Popcnt` to match the new F16C intrinsics.
- Deferred when we load the SLEEF builtins to where they are actually used, decreasing compile time with Burst by 4.29% on average.

### Fixed
- Fix an issue where a generic job instance (e.g `MyGenericJob<int>`) when used through a generic argument of a method or type would not be detected by the Burst compiler when building a standalone player.
- `[DlIimport("__Internal")]` for iOS now handled correctly. Fixes crashes when using native plugins on iOS.

### Removed

### Known Issues

## [1.3.0-preview.7] - 2020-03-16


### Added
- Added additional diagnostic for tracking Visual Studio location failures.
- Added an override to bypass link.exe discovery under certain conditions.
- Added a ldloc -> stloc optimization which improves compile times.
- More documentation on function pointers, specifically some performance considerations to be aware of when using them.

### Removed

### Changed
- Updated tools used for determining Visual Studio locations.

### Fixed
- Embedded Portable PDB handling improved.
- Fixed a case where our load/store optimizer would inadvertently combine a load/store into a cpblk where there were intermediate memory operations that should have been considered.
- Fixed a bug where the no-alias analysis would, through chains of complicated pointer math, deduce that a no-alias return (like from `UnsafeUtility.Malloc`) would not alias with itself.
- No longer log missing MonoPInvokeCallbackAttribute when running tests.

### Known Issues

## [1.3.0-preview.6] - 2020-03-12


### Added
- Experimental support for `Prefetch`, allowing users to request from the memory subsystem pointer addresses they intend to hit next. This functionality is guarded by the `UNITY_BURST_EXPERIMENTAL_PREFETCH_INTRINSIC` preprocessor define.

### Fixed
- Fix SSE `maxps` intrinsic would emit `maxss`

## [1.3.0-preview.5] - 2020-03-11


### Fixed
- `MemCpy` and `MemSet` performance regression in Burst 1.3.0.preview.4 (as was spotted by [@tertle](https://forum.unity.com/members/33474/)) has been fixed.
- Fix a crash when loading assembly with PublicKeyToken starting with a digit.
- Better handling of MonoPInvokeCallbackAttribute: no check for the namespace, don't print message on Mono builds.

### Changed
- Improved error message for typeof usage.

## [1.3.0-preview.4] - 2020-03-02


### Added
- Debug information for types.
- Debug information for local variables.
- Debug information for function parameters.
- Support for `fixed` statements. These are useful when interacting with `fixed` buffers in structs, to get at the pointer data underneath.
- A fast-math optimization for comparisons that benefits the [BurstBenchmarks](https://github.com/nxrighthere/BurstBenchmarks) that [nxrightthere](https://forum.unity.com/members/nxrighthere.568489/) has put together.
- DOTS Runtime Jobs will now generate both `MarshalToBurst` and `MarshalFromBurst` functions when job structs in .Net builds are not blittable.
- DOTS Runtime Job Marshalling generation is now controllable via the commandline switch `--generate-job-marshalling-methods`.

### Removed

### Changed
- Made it clear that the Burst aliasing intrinsics are tied to optimizations being enabled for a compilation.
- Restore unwind information for all builds.
- Print a info message if compiling a function pointer with missing MonoPInvokeCallback attribute (this can lead to runtime issues on IL2CPP with Burst disabled). The message will be converted to a warning in future releases.

### Fixed
- Fixed an issue where DOTS Runtime generated job marshalling functiosn may throw a `FieldAccessException` when scheduling private and internal job structs.
- Fix a bug that prevented entry point method names (and their declaring type names) from having a leading underscore.
- vector/array/pointer debug data now utilizes the correct size information.
- DOTS Runtime will now only generate job marshaling functions on Windows, as all other platforms rely on Mono which does not require job marshalling.
- `ldobj` / `stobj` of large structs being copied to stack-allocated variables could cause compile-time explosions that appeared to the user like the compiler had hung. Worked around these by turning them into memcpy's underneath in LLVM.
- Don't always use latest tool chain on certain platforms.
- Fix a crash when compiling job or function pointer that was previously cached, then unloaded, then reloaded.
- Fixed compiler error in array element access when index type is not `Int32`.
- Fix `set1_xxx` style x86 intrinsics generated compile time errors.

### Known Issues
- Native debugger feature is only available on windows host platform at the moment.

## [1.3.0-preview.3] - 2020-02-12


### Changed
- Changed how the inliner chooses to inline functions to give the compiler much more say over inlining decisions based on heuristics.
- Updated AOT requirements to be clearer about cross platform support.

### Added
- 1.3.0-preview.1 added support for desktop cross compilation, but the changelog forgot to mention it.

### Removed

### Fixed
- Documentation for the command line options to unity contained extra -
- Burst now exclusively uses the `<project>/Temp/Burst` folder for any temporary files it requires during compilation.
- Fix a regression that could break usage of native plugins.

### Known Issues

## [1.3.0-preview.2] - 2020-02-10

### Fixed
- Fix the error `Burst failed to compile the function pointer Int32 DoGetCSRTrampoline()` that could happen when loading a project using Burst with Burst disabled.

## [1.3.0-preview.1] - 2020-02-04

### Added
- Enabled lower precision variants for `pow`, `sin`, `cos`, `log`, `log2`, `log10`, `exp`, `exp2`, and `exp10` when `BurstPrecision.Low` is specified.
- Add CPU minimum and maximum target for desktop platforms Standalone Player builds.
- Append a newline between IRPassDiagnostic messages, fixes pass diagnostics readability in the inspector.
- Add a new attribute `[AssumeRange]` that lets users tag function parameters and returns of an integer type with a constrained range that the value is allowed to inhabit. `NativeArray.Length` and `NativeSlice.Length` have automatic detection that the property is always positive. This assumption feeds into the optimizer and can produce better codegen.
- Enabled support for DOTS Runtime SharedStatics. Due to the nature of DOTS Runtime, only the generic versions of `SharedStatic.GetOrCreate<TContext>` are supported.
- Add a new intrinsic `Unity.Burst.Intrinsics.Common.Pause()` which causes a thread pause to occur for the current thread. This is useful for spin-locks to stop over contention on the lock.
- Add some new Burst aliasing deductions to substantially improve the aliasing detection in the compiler, resulting in better codegen.
- Add syntax colouring to WASM.
- Add `IsCreated` to the `FunctionPointer` class to allow checks on whether a given function pointer has a valid (non null) pointer within it.
- Add AVX2 intrinsics
- Add some missing intrinsics from SSE, SSE2 and AVX
- Added explicit X86 intrinsics from SSE-AVX2.
- AVX and AVX2 CPU targets are now available for x64 AOT builds.
- Allow handle structs (structs with a single pointer/integer in them) to be inside another struct as long as they are the single member, as these require no ABI pain.
- Added support for `Interlocked.Read`.
- Added a new intrinsic `Common.umul128` which lets you get the low and high components of a 64-bit multiplication. This is especially useful for things like large hash creation.
- Menu option to allow all burst jobs to be more easily debugged in a native debugger.

### Removed

### Changed
- Upgraded Burst to use LLVM Version 9.0.1 by default, bringing the latest optimization improvements from the LLVM project.
- Upgraded Burst to use SLEEF 3.4.1, bringing the latest performance improvements to mathematics functions as used in Burst.
- Improved Burst performance in the Editor by caching compiled libraries on-disk, meaning that in subsequent runs of the Editor, assemblies that haven't changed won't be recompiled.
- Update the documentation of `CompileSynchronously` to advise against any general use of setting `CompileSynchronously = true`.
- Take the `Unity.Burst.CompilerServices.Aliasing` intrinsics out of experimental. These intrinsics form part of our strategy to give users more insight into how the compiler understands their code, by producing compiler errors when user expectations are not met. Questions like _'Does A alias with B?'_ can now be definitively answered for developers. See the **Aliasing Checks** section of the Burst documentation for information.
- Align disassembly instruction output in Inspector (x86/x64 only).
- Renamed `m128` to `v128`.
- Renamed `m256` to `v256`.
- BurstCompile(Debug=true), now modifies the burst code generator (reducing some optimisations) in order to allow a better experience in debugging in a native debugger.

### Fixed
- Fix a bug where floating-point != comparisons were using a stricter NaN-aware comparison than was required.
- Fix inspector for ARMV7_NEON target.
- Fix some issues with Burst AOT Settings, including changing the settings to be Enable rather than Disable.
- Fix an issue where WASM was being incorrectly shown in the disassembly view.
- Fixed an issue where if the `Unity.Entities.StaticTypeRegistry` assembly wasn't present in a build, Burst would throw a `NullReferenceException`.
- Fix issue with type conversion in m128/m256 table initializers.
- Fix inspector source line information (and source debug information) from being lost depending on inlining.
- Fix occasional poor code generation for on stack AVX2 variables.
- Fix `xor_ps` was incorrectly downcoded.
- Fix reference version of AVX2 64-bit variable shifts intrinsics.
- Fix reference version of SSE4.2 `cmpestrz`.
- Fix bitwise correctness issue with SSE4.2/AVX explicit rounding in CEIL mode for negative numbers that round to zero (was not correctly computing negative zero like the h/w).
- Fix calls to `SHUFFLE`, `SHUFFLE_PS` and similar macro-like functions would not work in non-entrypoint functions.
- Source location information was offset by one on occasions.
- Debug metadata is now tracked on branch/switch instructions.
- Fix poor error reporting when intrinsic immediates were not specified as literals.
- Fix basic loads and stores (using explicit calls) were not unaligned and sometimes non-temporal when they shouldn't be.
- Removed the  `<>c__DisplayClass_` infix that was inserted into every `Entities.ForEach` in the Burst inspector to clean up the user experience when searching for Entities.ForEach jobs.
- Fix background compile errors accessing X86 `MXCSR` from job threads.
- Fix possible `ExecutionEngineException` when resolving external functions.
- Fix linker output not being propagated through to the Editor console.

### Known Issues

## [1.2.0-preview.9] - 2019-11-06

- Fix compilation requests being lost when using asynchronous compilation.
- Prevent Burst compilation being toggled on while in play mode, either via "Enable Compilation" menu item or programmatically - was previously technically possible but produced unpredictable results.

## [1.2.0-preview.8] - 2019-11-01

- Fix a `NullReferenceException` happening in a call stack involving `CecilExtensions.IsDelegate(...)`.

## [1.2.0-preview.7] - 2019-10-30

- Many improvements to the Inspector:
  - New assembly syntax colorization!
  - Fix issue with menu settings being modified when opening the Inspector.
  - Make compile targets left pane resizable.
  - Fix vertical scrollbar size.
  - Add automatic refresh when selecting a target to compile.
- Fix an issue where `ref readonly` of a struct type, returned from a function, would cause a compiler crash.
- Add support for `Interlocked.Exchange` and `Interlocked.CompareExchange` for float and double arguments.
- Fix bug preventing iOS builds from working, if burst is disabled in AOT Settings.

## [1.2.0-preview.6] - 2019-10-16

- New multi-threaded compilation support when building a standalone player.
- Improve `BurstCompiler.CompileFunctionPointer` to compile asynchronously function pointers in the Editor.
- Improve of error codes and messages infrastructure.
- Upgraded Burst to use LLVM Version 8.0.1 by default, bringing the latest optimization improvements from the LLVM project.
- Fix issue with libtinfo5 missing on Linux.
- Fix possible NullReferenceException when an entry point function is calling another empty function.
- Fix an exception occurring while calculating the size of a struct with indirect dependencies to itself.
- Fix potential failure when loading MDB debugging file.
- Fix linker issue with folder containing spaces.
- Fix issue with package validation by removing ifdef around namespaces.
- Fix issue with an internal compiler exception related to an empty stack.

## [1.2.0-preview.5] - 2019-09-23

- Fix crashing issue during the shutdown of the editor.

## [1.2.0-preview.4] - 2019-09-20

- Fix a logging issue on shutdown.

## [1.2.0-preview.3] - 2019-09-20

- Fix potential logging of an error while shutting down the editor.

## [1.2.0-preview.2] - 2019-09-20

- New multi-threaded compilation of jobs/function pointers in the editor.
- Improve caching of compiled jobs/function pointers.
- Fix a caching issue where some jobs/function pointers would not be updated in the editor when updating their code.
- Fix an issue where type initializers with interdependencies were not executed in the correct order.
- Fix an issue with `Failed to resolve assembly Windows, Version=255.255.255.255...` when building for Xbox One.
- Fix compilation error on ARM32 when calling an external function.
- Fix an issue with function pointers that would generate invalid code if a non-blittable type is used in a struct passed by ref.
- Fix an issue with function pointers that would generate invalid code in case containers/pointers passed to the function are memory aliased.
- Report a compiler error if a function pointer is trying to be compiled without having the `[BurstCompile]` attribute on the method and owning type.

## [1.2.0-preview.1] - 2019-09-09

- Fix assembly caching issue, cache usage now conservative (Deals with methods that require resolving multiple assemblies prior to starting the compilation - generics).
- Fix Mac OS compatibility of Burst (10.10 and up) - fixes undefined symbol _futimens_.

## [1.1.3-preview.3] - 2019-09-02

- Query android API target level from player settings when building android standalone players.
- Add calli opcode support to support bindings to native code.

## [1.1.3-preview.2] - 2019-08-29

- Fix to allow calling [BurstDiscard] functions from static constructors.
- Correctly error if a DLLImport function uses a struct passed by value, but allow handle structs (structs with a single pointer/integer in them) as these require no ABI pain.
- Upgraded Burst to use LLVM Version 8 by default, bringing the latest optimisation improvements from the LLVM project.
- Added support for multiple LLVM versions, this does increase the package size, however it allows us to retain compatability with platforms that still require older versions of LLVM.
- Fix bug in assembly caching, subsequent runs should now correctly use cached jit code as appropriate.
- Add support for Lumin platform

## [1.1.3-preview.1] - 2019-08-26

- Add support for use of the MethodImpl(MethodImplOptions.NoOptimization) on functions.
- Fix an issue whereby static readonly vector variables could not be constructed unless using the constructor whose number of elements matched the width of the vector.
- Fix an issue whereby static readonly vector variables could not be struct initialized.
- Improve codegen for structs with explicit layout and overlapping fields.
- Fix a bug causing SSE4 instructions to be run on unsupported processors.
- Fix an issue where storing a pointer would fail as our type normalizer would cast the pointer to an i8.
- Begin to add Burst-specific aliasing information by instructing LLVM on our stack-allocation and global variables rules.

## [1.1.2] - 2019-07-26

- Fix an issue where non-readonly static variable would not fail in Burst while they are not supported.
- Fix issue with char comparison against an integer. Add partial support for C# char type.
- Improve codegen for struct layout with simple explicit layout.
- Fix NullReferenceException when using a static variable with a generic declaring type.
- Fix issue with `stackalloc` not clearing the allocated stack memory as it is done in .NET CLR.

## [1.1.1] - 2019-07-11

- Fix a compiler error when using a vector type as a generic argument of a NativeHashMap container.
- Disable temporarily SharedStatic/Execution mode for current 2019.3 alpha8 and before.
- Fix detection of Android NDK for Unity 2019.3.
- Update documentation for known issues.

## [1.1.0] - 2019-07-09

- Fix detection of Android NDK for Unity 2019.3.
- Update documentation for known issues.

## [1.1.0-preview.4] - 2019-07-05

- Burst will now report a compilation error when writing to a `[ReadOnly]` container/variable.
- Fix regression with nested generics resolution for interface calls.
- Fix issue for UWP with Burst generating non appcert compliant binaries.
- Fix issue when reading/writing vector types to a field of an explicit layout.
- Fix build issue on iOS, use only hash names for platforms with clang toolchain to mitigate issues with long names in LLVM IR.
- Allow calls to intrinsic functions (e.g `System.Math.Log`) inside static constructors.
- Improve performance when detecting if a method needs to be recompiled at JIT time.
- Fix an issue with explicit struct layout and vector types.

## [1.1.0-preview.3] - 2019-06-28

- Fix issue with generic resolution that could fail.
- Add support for readonly static data through generic instances.
- Add internal support for `SharedStatic<T>` for TypeManager.
- Add intrinsic support for `math.bitmask`.

## [1.1.0-preview.2] - 2019-06-20

- Fix issue where uninitialized values would be loaded instead for native containers containing big structs.
- Fix issue where noalias analysis would fail for native containers containing big structs.
- Fix issue when calling "internal" methods that take bool parameters.
- Add support for `MethodImplOptions.AggressiveInlining` to force inlining.
- Fix issue in ABITransform that would cause compilation errors with certain explicit struct layouts.
- Disable debug information generation for PS4 due to IR compatability issue with latest SDK.
- Implemented an assembly level cache for JIT compilation to improve iteration times in the Editor.
- Implement a hard cap on the length of symbols to avoid problems for platforms that ingest IR for AOT.
- Add support for `FunctionPointer<T>` usable from Burst Jobs via `BurstCompiler.CompileFunctionPointer<T>`.
- Add `BurstCompiler.Options` to allow to control/enable/disable Burst jobs compilation/run at runtime.
- Add `BurstRuntime.GetHashCode32<T>` and `GetHashCode64<T>` to allow to generate a hash code for a specified time from a Burst job.

## [1.0.0] - 2019-04-16

- Release stable version.

## [1.0.0-preview.14] - 2019-04-15

- Bump to mathematics 1.0.1
- Fix android ndk check on windows when using the builtin toolchain.
- Fix crash when accessing a field of a struct with an explicit layout through an embedded struct.
- Fix null pointer exception on building for android if editor version is less than 2019.1.
- Workaround IR compatibility issue with AOT builds on IOS.

## [1.0.0-preview.13] - 2019-04-12

- Fix linker error on symbol `$___check_bounds already defined`.
- Fix StructLayout Explicit size calculation and backing storage.

## [1.0.0-preview.12] - 2019-04-09

- Fix crash when accessing a NativeArray and performing in-place operations (e.g `nativeArray[i] += 121;`).

## [1.0.0-preview.11] - 2019-04-08

- Improve error logging for builder player with Burst.
- Fix NullReferenceException when storing to a field which is a generic type.

## [1.0.0-preview.10] - 2019-04-05

- Update known issues in the user manual.
- Improve user manual documentation about debugging, `[BurstDiscard]` attribute, CPU architectures supported...
- Fix an issue where Burst callbacks could be sent to the editor during shutdowns, causing an editor crash.
- Improve error messages for external tool chains when building for AOT.

## [1.0.0-preview.9] - 2019-04-03

- Fix an auto-vectorizer issue not correctly detecting the safe usage of NativeArray access when performing in-place operations (e.g `nativeArray[i] += 121;`).
- Add support for dynamic dispatch of functions based on CPU features available at runtime.
  - Fix issue when running SSE4 instructions on a pre-SSE4 CPU.
- Fix write access to `NativeArray<bool>`.
- Remove dependencies to C runtime for Windows/Linux build players (for lib_burst_generated.so/.dll).
- Updated API documentation.
- Update User manual.
- Static link some libraries into the Burst llvm wrapper to allow better support for some linux distros.

## [1.0.0-preview.8] - 2019-03-28

- Fix for iOS symbol names growing too long, reduced footprint of function names via pretty printer and a hash.

## [1.0.0-preview.7] - 2019-03-28

- Burst will now only generate debug information for AOT when targeting a Development Build.
- Added support for locating the build tools (standalone) for generating AOT builds on windows, without having to install Visual Studio complete.
- Fix Log Timings was incorrectly being passed along to AOT builds, causing them to fail.
- Fix editor crash if Burst aborted compilation half way through (because editor was being closed).
- Fix issue with job compilation that could be disabled when using the Burst inspector.
- Fix issue with spaces in certain paths (e.g. ANDROID_NDK_ROOT) when building for AOT.
- Restore behavior of compiling ios projects from windows with Burst, (Burst does not support cross compiling for ios) - we still generate a valid output project, but with no Burst code.
- Add support for Android embedded NDK.
- Fix issue where certain control flow involving object construction would crash the compiler in release mode.

## [1.0.0-preview.6] - 2019-03-17

- Fix invalid codegen with deep nested conditionals.
- Fix issue with Burst menu "Enable Compilation" to also disable cache jobs.
- Improve handling of PS4 toolchain detection.

## [1.0.0-preview.5] - 2019-03-16

- Fix regression with JIT caching that was not properly recompiling changed methods.
- Remove NativeDumpFlags from public API.
- Remove usage of PropertyChangingEventHandler to avoid conflicts with custom Newtonsoft.Json.
- Fix issue when a job could implement multiple job interfaces (IJob, IJobParallelFor...) but only the first one would be compiled.

## [1.0.0-preview.4] - 2019-03-15

- Fix "Error while verifying module: Invalid bitcast" that could happen with return value in the context of deep nested conditionals.
- Fix support for AOT compilation with float precision/mode.
- Fix fast math for iOS/PS4.
- Fix issue with double not using optimized intrinsics for scalars.
- Fix issue when loading a MDB file was failing when building a standalone player.
- Fix no-alias analysis that would be disabled in a standalone player if only one of the method was failing.
- Fix bug with explicit layout struct returned as a pointer by a property but creating an invalid store.
- Change `FloatPrecision.Standard` defaulting from `FloatPrecision.High` (ULP1) to `FloatPrecision.Medium` (ULP3.5).

## [1.0.0-preview.3] - 2019-03-14

- Fix compilation issue with uTiny builds.

## [1.0.0-preview.2] - 2019-03-13

- Fix no-alias warning spamming when building a standalone player.
- Improve the layout of the options/buttons for the inspector so that they at least attempt to layout better when the width is too small for all the buttons.
- Fix formatting of error messages so the Unity Console can correctly parse the location as a clickable item (Note however it does not appear to allow double clicking on absolute paths).
- Change Burst menu to Jobs/Burst. Improve order of menu items.
- Fix for AOTSettings bug related to StandaloneWindows vs StandaloneWindows64.

## [1.0.0-preview.1] - 2019-03-11

- Fix regression when resolving the type of generic used in a field.
- Fix linker for XboxOne, UWP.
- Fix performance codegen when using large structs.
- Fix codegen when a recursive function is involved with platform dependent ABI transformations.

## [0.2.4-preview.50] - 2019-02-27

- Fix meta file conflict.
- Fix changelog format.

## [0.2.4-preview.49] - 2019-02-27

- Move back com.unity.burst.experimental for function pointers support, but use internal modifier for this API.
- Restructure package for validation.

## [0.2.4-preview.48] - 2019-02-26

- Move back com.unity.burst.experimental for function pointers support, but use internal modifier for this API.

## [0.2.4-preview.47] - 2019-02-26

- Fix an issue during publish stage which was preventing to release the binaries.

## [0.2.4-preview.46] - 2019-02-26

- iOS player builds now use static linkage (to support TestFlight)  - Minimum supported Unity versions are 2018.3.6f1 or 2019.1.0b4.
- Fix a warning in Burst AOT settings.
- Enable forcing synchronous job compilation from menu.

## [0.2.4-preview.45] - 2019-02-07

- Disable Burst AOT settings support for unity versions before 2019.1.

## [0.2.4-preview.44] - 2019-02-06

- Fix incorrect conversions when performing subtraction with enums and floats.
- Fix compatability issue with future unity versions.
- Fix bug with ldfld bitcast on structs with explicit layouts.
- Guard against an issue resolving debug locations if the scope is global.

## [0.2.4-preview.43] - 2019-02-01

- Add preliminary support for Burst AOT settings in the player settings.
- Move BurstCompile (delegate/function pointers support) from com.unity.burst package to com.unity.burst.experimental package.
- Fix issue with stackalloc allocating a pointer size for the element type resulting in possible StackOverflowException.
- Add support for disabling Burst compilation from Unity editor with the command line argument `--burst-disable-compilation` .
- Add support for forcing synchronous compilation from Unity editor with the command line argument `--burst-force-sync-compilation`.
- Fix a compiler crash when generating debugging information.
- Fix invalid codegen involving ternary operator

## [0.2.4-preview.42] - 2019-01-22

- Fix a compilation error when implicit/explicit operators are used returning different type for the same input type.

## [0.2.4-preview.41] - 2019-01-17

- Fix codegen issue with Interlocked.Decrement that was instead performing an increment.
- Fix codegen issue for an invalid layout of struct with nested recursive pointer references.
- Fix for Fogbugz case : https://fogbugz.unity3d.com/f/cases/1109514/.
- Fix codegen issue with ref bool on a method argument creating a compiler exception.

## [0.2.4-preview.40] - 2018-12-19

- Fix bug when a write to a pointer type of an argument of a generic function.
- Breaking change of API: `Accuracy` -> `FloatPrecision`, and `Support` => `FloatMode`.
- Add `FloatMode.Deterministic` mode with early preview of deterministic mathematical functions.
- Fix bug with fonts in inspector being incorrectly reloaded.

## [0.2.4-preview.39] - 2018-12-06

- Add preview support for readonly static arrays typically used for LUT.
- Fix an issue with generics incorrectly being resolved in certain situations.
- Fix ARM32/ARM64 compilation issues for some instructions.
- Fix ARM compilation issues on UWP.
- Fix issue with math.compress.
- Add support for `ldnull` for storing a managed null reference to a ref field (e.g for DisposeSentinel).

## [0.2.4-preview.38] - 2018-11-17

- Fix issue when converting an unsigned integer constant to a larger unsigned integer (e.g (ulong)uint.MaxValue).
- Fix crash in editor when IRAnalysis can return an empty string .
- Fix potential crash of Cecil when reading symbols from assembly definition.

## [0.2.4-preview.37] - 2018-11-08

- Fix a crash on Linux and MacOS in the editor with dlopen crashing when trying to load burst-llvm (linux).

## [0.2.4-preview.36] - 2018-11-08

- Fix a crash on Linux and MacOS in the editor with dlopen crashing when trying to load burst-llvm (mac).

## [0.2.4-preview.35] - 2018-10-31

- Try to fix a crash on macosx in the editor when a job is being compiled by Burst at startup time.
- Fix Burst accidentally resolving reference assemblies.
- Add support for Burst for ARM64 when building UWP player.

## [0.2.4-preview.34] - 2018-10-12

- Fix compiler exception with an invalid cast that could occur when using pinned variables (e.g `int32&` resolved to `int32**` instead of `int32*`).

## [0.2.4-preview.33] - 2018-10-10

- Fix a compiler crash with methods incorrectly being marked as external and throwing an exception related to ABI.

## [0.2.4-preview.32] - 2018-10-04

- Fix codegen and linking errors for ARM when using mathematical functions on plain floats.
- Add support for vector types GetHashCode.
- Add support for DllImport (only compatible with Unity `2018.2.12f1`+ and ` 2018.3.0b5`+).
- Fix codegen when converting uint to int when used in a binary operation.

## [0.2.4-preview.31] - 2018-09-24

- Fix codegen for fmodf to use inline functions instead.
- Add extended disassembly output to the Burst inspector.
- Fix generic resolution through de-virtualize methods.
- Fix bug when accessing float3.zero. Prevents static constructors being considered intrinsics.
- Fix NoAlias attribute checking when generics are used.

## [0.2.4-preview.30] - 2018-09-11

- Fix IsValueType throwing a NullReferenceException in case of using generics.
- Fix discovery for Burst inspector/AOT methods inheriting from IJobProcessComponentData or interfaces with generics.
- Add `[NoAlias]` attribute.
- Improved codegen for csum.
- Improved codegen for abs(int).
- Improved codegen for abs on floatN/doubleN.

## [0.2.4-preview.29] - 2018-09-07

- Fix issue when calling an explicit interface method not being matched through a generic constraint.
- Fix issue with or/and binary operation on a bool returned by a function.

## [0.2.4-preview.28] - 2018-09-05

- Fix a compilation issue when storing a bool returned from a function to a component of a bool vector.
- Fix AOT compilation issue with a duplicated dictionary key.
- Fix settings of ANDROID_NDK_ROOT if it is not setup in Unity Editor.

## [0.2.4-preview.27] - 2018-09-03

- Improve detection of jobs within nested generics for AOT/Burst inspector.
- Fix compiler bug of comparison of a pointer to null pointer.
- Fix crash compilation of sincos on ARM (neon/AARCH64).
- Fix issue when using a pointer to a VectorType resulting in an incorrect access of a vector type.
- Add support for doubles (preview).
- Improve AOT compiler error message/details if the compiler is failing before the linker.

## [0.2.4-preview.26] - 2018-08-21

- Added support for cosh, sinh and tanh.

## [0.2.4-preview.25] - 2018-08-16

- Fix warning in unity editor.

## [0.2.4-preview.24] - 2018-08-15

- Improve codegen of math.compress.
- Improve codegen of math.asfloat/asint/asuint.
- Improve codegen of math.csum for int4.
- Improve codegen of math.count_bits.
- Support for lzcnt and tzcnt intrinsics.
- Fix AOT compilation errors for PS4 and XboxOne.
- Fix an issue that could cause wrong code generation for some unsafe ptr operations.

## [0.2.4-preview.23] - 2018-07-31

- Fix bug with switch case to support not only int32.

## [0.2.4-preview.22] - 2018-07-31

- Fix issue with pointers comparison not supported.
- Fix a StackOverflow exception when calling an interface method through a generic constraint on a nested type where the declaring type is a generic.
- Fix an issue with EntityCommandBuffer.CreateEntity/AddComponent that could lead to ArgumentException/IndexOutOfRangeException.

## [0.2.4-preview.21] - 2018-07-25

- Correct issue with Android AOT compilation being unable to find the NDK.

## [0.2.4-preview.20] - 2018-07-05

- Prepare the user documentation for a public release.

## [0.2.4-preview.19] - 2018-07-02

- Fix compilation error with generics when types are coming from different assemblies.

## [0.2.4-preview.18] - 2018-06-26

- Add support for subtracting pointers.

## [0.2.4-preview.17] - 2018-06-25

- Bump only to force a new version pushed.

## [0.2.4-preview.16] - 2018-06-25

- Fix AOT compilation errors.

## [0.2.4-preview.15] - 2018-06-25

- Fix crash for certain access to readonly static variable.
- Fix StackOverflowException when using a generic parameter type into an interface method.

## [0.2.4-preview.14] - 2018-06-23

- Fix an issue with package structure that was preventing Burst to work in Unity.

## [0.2.4-preview.13] - 2018-06-22

- Add support for Burst timings menu.
- Improve codegen for sin/cos.
- Improve codegen when using swizzles on vector types.
- Add support for sincos intrinsic.
- Fix AOT deployment.

## [0.2.4-preview.12] - 2018-06-13

- Fix a bug in codegen that was collapsing methods overload of System.Threading.Interlocked to the same method.

## [0.2.4-preview.11] - 2018-06-05

- Fix exception in codegen when accessing readonly static fields from different control flow paths.

## [0.2.4-preview.10] - 2018-06-04

- Fix a potential stack overflow issue when a generic parameter constraint on a type is also referencing another generic parameter through a generic interface constraint
- Update to latest Unity.Mathematics:
  - Fix order of parameters and codegen for step functions.

## [0.2.4-preview.9] - 2018-05-29

- Fix bug when casting an IntPtr to an enum pointer that was causing an invalid codegen exception.

## [0.2.4-preview.8] - 2018-05-24

- Breaking change: Move Unity.Jobs.Accuracy/Support to Unity.Burst.
- Deprecate ComputeJobOptimizationAttribute in favor of BurstCompileAttribute.
- Fix bug when using enum with a different type than int.
- Fix bug with IL stind that could lead to a memory corruption.

## [0.2.4-preview.7] - 2018-05-22

- Add support for nested structs in SOA native arrays.
- Add support for arbitrary sized elements in full SOA native arrays.
- Fix bug with conversion from signed/unsigned integers to signed numbers (integers & floats).
- Add support for substracting pointers at IL level.
- Improve codegen with pointers arithmetic to avoid checking for overflows.

## [0.2.4-preview.6] - 2018-05-11

- Remove `bool1` from mathematics and add proper support in Burst.
- Add support for ARM platforms in the Burst inspector UI.

## [0.2.4-preview.5] - 2018-05-09

- Add support for readonly static fields.
- Add support for stackalloc.
- Fix potential crash on MacOSX when using memset is used indirectly.
- Fix crash when trying to write to a bool1*.
- Fix bug with EnableBurstCompilation checkbox not working in Unity Editor.

## [0.2.4-preview.4] - 2018-05-03

- Fix an issue on Windows with `DllNotFoundException` occurring when trying to load `burst-llvm.dll` from a user profile containing unicode characters in the folder path.
- Fix an internal compiler error occurring with IL dup instruction.

## [0.2.4-preview.3] - 2018-05-03

- Add support for struct with an explicit layout.
- Fix noalias regression (that was preventing the auto-vectorizer to work correctly on basic loops).

## 0.2.3 (21 March 2018)

- Improve error messages for static field access.
- Improve collecting of compilable job by trying to collect concrete job type instances (issue #23).

## 0.2.2 (19 March 2018)

- Improve error messages in case using `is` or `as` cast in C#.
- Improve error messages if a static delegate instance is used.
- Fix codegen error when converting a byte/ushort to a float.
