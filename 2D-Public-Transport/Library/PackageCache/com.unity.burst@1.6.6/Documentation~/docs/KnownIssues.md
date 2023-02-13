# Known Issues

- The maximum target CPU is currently hardcoded per platform. For standalone builds that target desktop platforms (Windows/Linux/macOS) you can choose the supported targets via [Burst AOT Settings](StandalonePlayerSupport.md#burst-aot-settings). For other platforms see the table above.
- Building iOS player from Windows will not use Burst, (see [Burst AOT Requirements](StandalonePlayerSupport.md#burst-aot-requirements))
- Building Android player from Linux will not use Burst, (see [Burst AOT Requirements](StandalonePlayerSupport.md#burst-aot-requirements))
- Building from an Xcode project generated via "Create Xcode Project" will not use Burst.
- If you change the Burst package version (via update for example), you need to close and restart the editor. A warning is now displayed to this effect.
- Struct with explicit layout can generate non optimal native code.
- `BurstCompiler.SetExecutionMode` does not affect the runtime yet for deterministic mode.
- Burst does not support scheduling generic Jobs through generic methods. While this would work in the editor, it will not work in the standalone player.
- Assemblies whose path contain a semicolon break Burst.

Some of these issues may be resolved in a future release of Burst.

## Known issues with `DllImport`

- `DllImport` is only supported for [native plugins](https://docs.unity3d.com/Manual/NativePlugins.html), not platform-dependent libraries like `kernel32.dll`.

## Known issues with debugging/profiling

- Lambda captures on `Entity.ForEach()` are not discovered for debugging data, so you won't be able to inspect variables originating from these.
- Structs that utilize `LayoutKind=Explicit`, and have overlapping fields, are represented by a struct that will hide one of the overlaps. In the future they will be represented as a union of structs, to allow inspection of fields that overlap.
- Function parameters are currently readonly from a debugging point of view. They are recorded to a stack argument during the prologue. Changing their value in the debugger may not have an affect.
- Due to the way we build code for Standalone Players, you will need to instruct the profiling/debugging tool where to locate the symbols (Point the tool to the folder containing the lib_burst_generated files (usually Plugins))
- Windows internal native debugging APIs have a restriction on the number of messages that are sent when attaching a native debugger (e.g. Visual Studio/WinDbg) :
  Essentially, this restricts the number of modules (DLLs) sent to the debugger on attaching, the default limit appears to be around 500. So while this normally would not be a problem, Burst in the editor generates 1 DLL per job/function pointer, and these can soon add up. In the future this behaviour will be changed, for now you can workaround the issue in one of two ways :
  - Attach the native debugger as early as possible (e.g. when launching unity), this will mean you are attached before burst begins to load the modules, and because burst loads the modules in turn, the debugger does not miss notifications due to receiving too many at once.
  - Alternatively, you can increase the size of the default cap by editing a registry key : In `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager`, edit or add a `DWORD DebuggerMaxModuleMsgs` with a value of e.g. 2048, and then restart windows. Now Visual Studio will can be sent upto 2048 modules at once, so attaching to the editor after burst has already loaded its modules won't miss any (assuming <2048 modules).
