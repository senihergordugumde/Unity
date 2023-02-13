# Debugging and Profiling

Burst now provides support for debugging and/or profiling using a native debugger (for instance - Visual Studio Community) or profiling tool (for instance - Instruments). Managed debugging of Burst compiled code is currently not supported.

# Managed debugging

> If you wish to use a managed debugger to debug a job, you will need to disable the Burst compiler or comment the `[BurstCompile]` attribute from your job and attach a **regular .NET managed debugger**

# Native debugging

Burst compiled code can be debugged using a native debugger by simply attaching the native debugger to the Unity process. However, due to the optimisations employed by Burst, you will generally find it easier to debug by ensuring your code is compiled with Native debuggers in mind.

You can do this either via the [Jobs Menu](QuickStart.md#jobs-burst-menu) which will compile the code with native debugging enabled globally (this disables optimizations, so it will impact performance of Burst code).

Alternatively, you can use the `Debug = true` option in the `[BurstCompile]` attribute for your job e.g.

```c#
[BurstCompile(Debug = true)]
public struct MyJob : IJob
{
    // ...
}
```

Which will then only affect optimizations (and debuggability) on that job. Note that currently Standalone Player builds will also pick up the `Debug` flag, so standalone builds can be debugged this way too.

Burst also supports code-based breakpoints via `System.Diagnostics.Debugger.Break()` which will generate a debug trap into the code. Note that if you do this you should ensure you have a debugger attached to intercept the break. At present the breakpoints will trigger whether a debugger is attached or not.

Burst adds information to track local variables, function parameters and breakpoints. If your debugger supports conditional breakpoints, these are preferable to inserting breakpoints in code, since they will only fire when a debugger is attached.

[Known issues with debugging/profiling](KnownIssues.md#known-issues-with-debuggingprofiling)
