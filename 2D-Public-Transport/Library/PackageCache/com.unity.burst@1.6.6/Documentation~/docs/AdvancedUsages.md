# Advanced Usages

# BurstDiscard Attribute

When running some code in the full C# (not inside Burst compiled code), you may want to use some managed objects, but you would like to not compile these portions of code when compiling within Burst.

To mitigate this, you can use the `[BurstDiscard]` attribute on a method:

```c#
[BurstCompile]
public struct MyJob : IJob
{
    public void Execute()
    {
        // Only executed when running from a full .NET runtime
        // this method call will be discard when compiling this job with
        // [BurstCompile] attribute
        MethodToDiscard();
    }

    [BurstDiscard]
    private static void MethodToDiscard(int arg)
    {
        Debug.Log($"This is a test: {arg}");
    }
}
```

Note that a method with `[BurstDiscard]` cannot have a return value.

You can have a `ref` or `out` parameter, which has a common use of:

```c#
[BurstDiscard]
private static void SetIfManaged(ref bool b) => b = false;

private static bool IsBurst()
{
    var b = true;
    SetIfManaged(ref b);
    return b;
}
```

Which lets users have some code that is aware whether it is running on Burst or managed.

# Synchronous Compilation

By default, the Burst compiler in the editor will compile the jobs asynchronously.

You can change this behavior by setting `CompileSynchronously = true` for the `[BurstCompile]` attribute:

```c#
[BurstCompile(CompileSynchronously = true)]
public struct MyJob : IJob
{
    // ...
}
```

When running a Burst job in the editor, the first attempt to call the job will cause the asynchronous compilation of the Burst job to be kicked off in the background, while running the managed C# job in the mean time. This minimizes any frame hitching and keeps the experience for you and your users responsive.

When `CompileSynchronously = true` is set, no asynchronous compilation can occur. Burst is focused on providing highly performance oriented code-generation and as a result will take a little longer than a traditional JIT to compile. Crucially this pause for compilation _will affect the current running frame_, meaning that hitches can occur and it could provide an unresponsive experience for users. In general, the only legitimate uses of `CompileSynchronously = true` are:

- If you have a long running job that will only run once, the performance of the compiled code could out-weigh the cost of doing the compilation.
- If you are profiling a Burst job and thus want to be certain that the code that is being tested is from the Burst compiler. In this scenario you should perform a warmup to throw away any timing measurements from the first call to the job as that would include the compilation cost and skew the result.
- If you suspect that there are some crucial differences between managed and Burst compiled code. This is really only used as a debugging aid, as the Burst compiler strives to match any and all behaviour that managed code could produce.

# Disable Safety Checks

Burst allows a user to mark a job or function-pointer as not requiring safety checks:

```c#
[BurstCompile(DisableSafetyChecks = true)]
public struct MyJob : IJob
{
    // ...
}
```

When set, Burst will remove all safety check code, resulting in code-generation that is generally faster. This option is _dangerous_ though as you really need to be certain that you are using containers in a safe fashion.

This option has some interactions with the global `Enable Safety Checks` option in the Burst menu:

- If `Enable Safety Checks` is set to `On`, safety checks will be enabled for all Burst-compiled code except those marked explicitly with `DisableSafetyChecks = true`.
- If `Enable Safety Checks` is set to `Force On`, all code _even that marked with_ `DisableSafetyChecks = true` will be compiled with safety checks. This option even allows users to enable safety checks in any downstream packages they depend on so that if they encounter some unexpected behaviour, they can first check that the safety checks would not have caught it.

# Optimization Choices

Burst supports an `OptimizeFor` field on `BurstCompile` that lets users tell the compiler what should be the primary aim when optimizing a specific job or function-pointer:

```c#
[BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]
public struct MyJob : IJob
{
    // ...
}
```

There are a few options:

- `Balanced` - the default, the compiler should optimize for code that runs fast, but still keeping compile time as low as possible.
- `Performance` - the compiler should optimize the job focusing on making the resulting code run as fast as possible.
- `Size` - the compiler should focus on making the code generated as small as possible.
- `FastCompilation` - the compiler should do some optimizations, but focus on compiling the code as fast as possible.

Note that when running with `FastCompilation` no vectorization, inlining, or loop optimizations will be performed.

# Function Pointers

It is often required to work with dynamic functions that can process data based on other data states. In that case, a user would expect to use C# delegates, but because in Burst these delegates are managed objects, we need to provide a HPC# compatible alternative. In that case you can use `FunctionPointer<T>`.

First you need identify the static functions that will be compiled with Burst:
- add a `[BurstCompile]` attribute to these functions
- add a `[BurstCompile]` attribute to the containing type. This attribute is only here to help the Burst compiler look for static methods with `[BurstCompile]` attribute
- create the "interface" of these functions by declaring a delegate
- add a `[MonoPInvokeCallbackAttribute]` attribute to the functions, as it is required to work properly with IL2CPP:

```c#
// Instruct Burst to look for static methods with [BurstCompile] attribute
[BurstCompile]
class EnclosingType {
    [BurstCompile]
    [MonoPInvokeCallback(typeof(Process2FloatsDelegate))]
    public static float MultiplyFloat(float a, float b) => a * b;

    [BurstCompile]
    [MonoPInvokeCallback(typeof(Process2FloatsDelegate))]
    public static float AddFloat(float a, float b) => a + b;

    // A common interface for both MultiplyFloat and AddFloat methods
    public delegate float Process2FloatsDelegate(float a, float b);
}
```

Then you need to compile these function pointers from regular C# code:

```c#
    // Contains a compiled version of MultiplyFloat with Burst
    FunctionPointer<Process2FloatsDelegate> mulFunctionPointer = BurstCompiler.CompileFunctionPointer<Process2FloatsDelegate>(MultiplyFloat);

    // Contains a compiled version of AddFloat with Burst
    FunctionPointer<Process2FloatsDelegate> addFunctionPointer = BurstCompiler.CompileFunctionPointer<Process2FloatsDelegate>(AddFloat);
```

Lastly, you can use these function pointers directly from a Job by passing them to the Job struct directly:

```c#
    // Invoke the function pointers from HPC# jobs
    var resultMul = mulFunctionPointer.Invoke(1.0f, 2.0f);
    var resultAdd = addFunctionPointer.Invoke(1.0f, 2.0f);
``` 

Note that you can also use these function pointers from regular C# as well, but it is highly recommended (for performance reasons) to cache the `FunctionPointer<T>.Invoke` property (which is the delegate instance) to a static field.

```c#
    private readonly static Process2FloatsDelegate mulFunctionPointerInvoke = BurstCompiler.CompileFunctionPointer<Process2FloatsDelegate>(MultiplyFloat).Invoke;

    // Invoke the delegate from C#
    var resultMul = mulFunctionPointerInvoke(1.0f, 2.0f);
```

> A few important additional notes:
>
> - Function pointers are compiled asynchronously for jobs by default. You can still force a synchronous compilation of function pointers by specifying this via the `[BurstCompile(SynchronousCompilation = true)]`.
> - Function pointers have limited support for exceptions. As is the case for jobs, exceptions only work in the editor (`2019.3+` only) and they will result in a crash if they are used within a Standalone Player. It is recommended not to rely on any logic related to exception handling when working with function pointers.
> - Using Burst-compiled function pointers from C# could be slower than their pure C# version counterparts if the function is too small compared to the cost of P/Invoke interop.
> - Function pointers don't support generic delegates.
> - Argument and return types are subject to the same restrictions as described for [`DllImport` and internal calls](CSharpLanguageSupport_BurstIntrinsics.md#dllimport-and-internal-calls).
> - You are strongly advised NOT to wrap `BurstCommpiler.CompileFunctionPointer<T>` within another open generic method, doing so prevents burst from being able to apply required attributes to the delegate and perform additional safety analysis, (and potential optimizations). 
> - Interoperability of function pointers with IL2CPP requires `System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute` on the delegate, with the calling convention set to `CallingConvention.Cdecl`, burst will automatically add this attribute to delegates that are used with `BurstCompiler.CompileFunctionPointer<T>`.

## Performance Considerations

If you are ever considering using Burst's function pointers, you should _always_ first consider whether a job would be better. Jobs are the most optimal way to run code produced by the Burst compiler for a few reasons:

- The superior aliasing calculations that Burst can provide with a job because of the rules imposed by the job safety system allow for much more optimizations by default.
- You cannot pass most of the `[NativeContainer]` structs like `NativeArray` directly to function pointers, only via Job structs. The native container structs contain managed objects for safety checks that the Burst compiler can work around when compiling jobs, but not for function pointers.
- Function pointers hamper the compiler's ability to optimize across functions.

Let's look at an example of how _not_ to use function pointers in Burst:

```c#
[BurstCompile]
public class MyFunctionPointers
{
    public unsafe delegate void MyFunctionPointerDelegate(float* input, float* output);

    [BurstCompile]
    public static unsafe void MyFunctionPointer(float* input, float* output)
    {
        *output = math.sqrt(*input);
    }
}

[BurstCompile]
struct MyJob : IJobParallelFor
{
     public FunctionPointer<MyFunctionPointers.MyFunctionPointerDelegate> FunctionPointer;

    [ReadOnly] public NativeArray<float> Input;
    [WriteOnly] public NativeArray<float> Output;

    public unsafe void Execute(int index)
    {
        var inputPtr = (float*)Input.GetUnsafeReadOnlyPtr();
        var outputPtr = (float*)Output.GetUnsafePtr();
        FunctionPointer.Invoke(inputPtr + index, outputPtr + index);
    }
}
```

In this example we've got a function pointer that is computing `math.sqrt` from an input pointer, and storing it to an output pointer. The `MyJob` job is then feeding this function pointer sourced from two `NativeArray`s. There are a few major performance problems with this example:

- The function pointer is being fed a single scalar element, thus the compiler cannot vectorize. This means you are losing 4-8x performance straight away from a lack of vectorization.
- The `MyJob` knows that the `Input` and `Output` native arrays cannot alias, but this information is not communicated to the function pointer.
- There is a non-zero cost to constantly branching to a function pointer somewhere else in memory. Modern processors do a decent job at eliding this cost, but it is still non-zero.

If you feel like you **must** use function pointers, then you should **always** process batches of data in the function pointer. Let's modify the example above to do just that:

```c#
[BurstCompile]
public class MyFunctionPointers
{
    public unsafe delegate void MyFunctionPointerDelegate(int count, float* input, float* output);

    [BurstCompile]
    public static unsafe void MyFunctionPointer(int count, float* input, float* output)
    {
        for (int i = 0; i < count; i++)
        {
            output[i] = math.sqrt(input[i]);
        }
    }
}

[BurstCompile]
struct MyJob : IJobParallelForBatch
{
     public FunctionPointer<MyFunctionPointers.MyFunctionPointerDelegate> FunctionPointer;

    [ReadOnly] public NativeArray<float> Input;
    [WriteOnly] public NativeArray<float> Output;

    public unsafe void Execute(int index, int count)
    {
        var inputPtr = (float*)Input.GetUnsafeReadOnlyPtr() + index;
        var outputPtr = (float*)Output.GetUnsafePtr() + index;
        FunctionPointer.Invoke(count, inputPtr, outputPtr);
    }
}
```

In our modified `MyFunctionPointer` you can see that it takes a `count` of elements to process, and loops over the `input` and `output` pointers to do many calculations. The `MyJob` becomes an `IJobParallelForBatch`, and the `count` is passed directly into the function pointer. This is better for performance:

- You now get vectorization in the `MyFunctionPointer` call.
- Because you are processing `count` items per function pointer, any cost of calling the function pointer is reduced by `count` times (EG. if you run a batch of 128, the function pointer cost is 1/128th per `index` of what it was previously).
- Doing the batching above realized a 1.53x performance gain over not batching, so it's a big win.

The best thing you can do though is just to use a job - this gives the compiler the most visibility over what you want it to do, and the most opportunities to optimize:

```c#
[BurstCompile]
struct MyJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> Input;
    [WriteOnly] public NativeArray<float> Output;

    public unsafe void Execute(int index)
    {
        Output[i] = math.sqrt(Input[i]);
    }
}
```

The above will run 1.26x faster than the batched function pointer example, and 1.93x faster than the non-batched function pointer examples above. The compiler has perfect aliasing knowledge and can make the broadest modifications to the above. Note: this code is also _significantly_ simpler than either of the function pointer cases, and shows that often the simplest solution provides the performance-by-default that Burst so strives for.

# Shared Static

Burst has basic support for accessing static readonly data, but if you want to share static mutable data between C# and HPC#, you need to use the `SharedStatic<T>` struct.

Let's take the example of accessing an `int` static field that could be changed by both C# and HPC#:

```C#
    public abstract class MutableStaticTest
    {
        public static readonly SharedStatic<int> IntField = SharedStatic<int>.GetOrCreate<MutableStaticTest, IntFieldKey>();

        // Define a Key type to identify IntField
        private class IntFieldKey {}
    }
```     

that can then be accessed from C# and HPC#:

```C#
    // Write to a shared static 
    MutableStaticTest.IntField.Data = 5;
    // Read from a shared static
    var value = 1 + MutableStaticTest.IntField.Data;
``` 

> A few important additional notes:
>
> - The type of the data is defined by the `T` in `SharedStatic<T>`.
> - In order to identify a static field, you need to provide a context for it: the common way to solve this is to create a key for both the containing type (e.g `MutableStaticTest` in our example above) and to identify the field (e.g `IntFieldKey` class in our example) and by passing these classes as generic arguments of `SharedStatic<int>.GetOrCreate<MutableStaticTest, IntFieldKey>()`.
> - It is recommended to always initialize the shared static field in C# from a static constructor before accessing it from HPC#. Not initializing the data before accessing it can lead to an undefined initialization state.

# Assembly-Level BurstCompile

Burst also supports an **assembly** level `BurstCompile` attribute:

```c#
[assembly: BurstCompile(CompileSynchronously = true)]
```

Applying a `BurstCompile` attribute to the assembly means that you can set options for all Burst jobs and function-pointers within an assembly, using a single attribute. For example, let's say you've got an assembly that contains just gameplay code, and this gameplay code needs to run fast (you are using Burst after all!), but you are more interested in quicker iterations using Burst. For this assembly you can put:

```c#
[assembly: BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]
```

Which will mean Burst will compile the code as fast as it possibly can, allowing users to iterate on gameplay code much more quickly. Crucially, this means that other assemblies will compile as they did before, allowing users much more control on how Burst is working with your code.

Assembly-level `BurstCompile` attributes iterate with any job or function-pointer attribute, and also with any globally set options from the Burst editor menu:

1. Any editor-menu setting takes precedence. For instance, if you set `Native Debug Compilation` on from the editor-menu, you'll always get code that is ready to be debugged.
2. Any `BurstCompile` attribute on a job or function-pointer then is checked next. If you had `CompileSynchronously = true` in the `BurstCompile`, then you'll get synchronous compilation.
3. Otherwise, any remaining settings are sourced from any assembly-level attribute.

Here's an example:

```c#
[assembly: BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]

// This job will be optimized for fast-compilation, because the per-assembly BurstCompile asked for it!
[BurstCompile]
struct AJob : IJob
{
    // ...
}

// This job will be optimized for size, because the per-job BurstCompile asked for it!
[BurstCompile(OptimizeFor = OptimizeFor.Size)]
struct BJob : IJob
{
    // ...
}
```

# Dynamic dispatch based on runtime CPU features 

For all `x86`/`x64` CPU desktop platforms, Burst will dispatch jobs to different versions compiled by taking into account CPU features available at runtime. 

Currently for `x86` and `x64` CPUs, Burst is supporting `SSE2` and `SSE4` instruction sets at runtime only. 

For example, with dynamic CPU dispatch, if your CPU supports `SSE3` and below, Burst will select `SSE2` automatically.

See the table in the section [Burst AOT Requirements](StandalonePlayerSupport.md#burst-aot-requirements) for more details about the supported CPU architectures.

