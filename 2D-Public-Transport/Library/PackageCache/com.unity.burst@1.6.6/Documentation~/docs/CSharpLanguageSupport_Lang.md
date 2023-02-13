# Language Support

Burst supports most of the expressions and statements:

- Regular C# control flows:
  - `if`/`else`/`switch`/`case`/`for`/`while`/`break`/`continue`.
- Extension methods.
- Unsafe code, pointer manipulation...etc.
- Instance methods of structs.
- By ref/out parameters.
- [`DllImport` and internal calls](CSharpLanguageSupport_BurstIntrinsics.md#dllimport-and-internal-calls).
- Limited support for `throw` expressions, assuming simple throw patterns (e.g `throw new ArgumentException("Invalid argument")`). In that case, we will try to extract the static string exception message to include it in the generated code.
- Some special IL opcodes like `cpblk`, `initblk`, `sizeof`.
- Loading from static readonly fields.
- Support for `fixed` statements.
- Support for `try`/`finally` and associated IDisposable patterns (`using` and `foreach`)
  - Note that if an exception occurs, the behavior will differ from .NET. In .NET, if an exception occurs inside a `try` block, control flow would go to the `finally` block. In Burst, if an exception occurs whether inside or outside a `try` block, the currently running job or function pointer will terminate immediately.
- [Partial support for strings and `Debug.Log`](#partial-support-for-strings-and-debuglog).
- [Support for strings and `ProfilerMarker`](#support-for-strings-and-profilermarker).

Burst also provides alternatives for some C# constructions not directly accessible to HPC#:

- [Function pointers](AdvancedUsages.md#function-pointers) as an alternative to using delegates within HPC#
- [Shared Static](AdvancedUsages.md#shared-static) to access static mutable data from both C# and HPC#

Burst does not support:

- Catching exceptions `catch` in a `try`/`catch`.
- Storing to static fields except via [Shared Static](AdvancedUsages.md#shared-static)
- Any methods related to managed objects (e.g string methods...etc.)

## Static readonly fields and static constructors
Burst will try to evaluate all static fields and all static constructors at compile-time.
The same base language is supported at compile-time, with some exceptions:

- Managed arrays and strings are supported.
- Calling external functions and function pointers is not supported.
- Only some intrinsics are supported:
  - Assertions:
    - `UnityEngine.Debug.Assert`
    - `NUnit.Framework.Assert.AreEqual`
    - `NUnit.Framework.Assert.AreNotEqual`
  - `Unity.Burst.BurstCompiler.IsEnabled`
  - `Unity.Burst.BurstRuntime.GetHashCode32` and `Unity.Burst.BurstRuntime.GetHashCode64`
  - Vector type construction

Simple throw patterns are also supported. Exceptions thrown during evaluation become compiler errors.

All the static fields and the static constructor for a given struct are evaluated together. Having non-readonly
static fields in a Burst compiled struct will therefore cause a compilation error, since only readonly static fields are supported.
If any static field or the static constructor fails to evaluate then all of them fail to evaluate for that struct. When compile-time evaluation fails
Burst will try to fall back to compiling all static initialization code into an initialization function
to be called once at runtime. This means that if code fails compile time evaluation it needs to be
Burst compatible, otherwise compilation will fail. An exception to this is that there's still limited support
for initializing static readonly array fields as long as they are initialized from either an array constructor or from static data:
- `static readonly int[] MyArray0 = { 1, 2, 3, .. };`
- `static readonly int[] MyArray1 = new int[10];`

## Directly Calling Burst Compiled Code

Burst supports the ability to call out to Burst compiled methods directly from managed code (`2019.4+` only). Calling generic methods or methods whose declaring type is generic is not supported, otherwise the rules as for [Function pointers](AdvancedUsages.md#function-pointers) apply. However as a user you don't need to worry about the extra boiler plate needed for function pointers.

As an example here is a utility class which is Burst compiled (notice because we are using structs we are passing by reference as per the Function Pointer rules).

```c#
[BurstCompile]
public static class MyBurstUtilityClass
{
    [BurstCompile]
    public static void BurstCompiled_MultiplyAdd(in float4 mula, in float4 mulb, in float4 add, out float4 result)
    {
        result = mula * mulb + add;
    }
}
```

Now we can utilise this method from our Managed code e.g. 

```c#
public class MyMonoBehaviour : MonoBehaviour
{
    void Start()
    {
        var mula = new float4(1, 2, 3, 4);
        var mulb = new float4(-1,1,-1,1);
        var add = new float4(99,0,0,0);
        MyBurstUtilityClass.BurstCompiled_MultiplyAdd(mula, mulb, add, out var result);
        Debug.Log(result);
    }
}
```

and if we were to run (with the managed script attached to an object), we should now see in the log `float4(98f, 2f, -3f, 4f)`.

This is achieved by using IL Post Processing in order to automatically transform the code into a function pointer and call, which is why you must adhere to the rules of [Function pointers](AdvancedUsages.md#function-pointers).

You can disable the direct call transformation by adding `DisableDirectCall = true` to the BurstCompile options, which will prevent the Post Processor from running on the code. E.g.

```c#
[BurstCompile]
public static class MyBurstUtilityClass
{
    [BurstCompile(DisableDirectCall = true)]
    public static void BurstCompiled_MultiplyAdd(in float4 mula, in float4 mulb, in float4 add, out float4 result)
    {
        result = mula * mulb + add;
    }
}
```

## Throw and Exceptions

Burst supports `throw` expressions for exceptions in the Editor (`2019.3+` only), but crucially **does not** in Standalone Player builds. Exceptions in Burst are to be used solely for _exceptional_ behavior. To ensure that code does not end up relying on exceptions for things like general control flow, Burst will produce the following warning on code that tries to `throw` within a method not attributed by `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]`:

> Burst warning BC1370: An exception was thrown from a function without the correct [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")] guard. Exceptions only work in the editor and so should be protected by this guard

## Partial support for strings and `Debug.Log`

Burst provides partial support for using strings in the following two main scenarios:

- `Debug.Log` (see below)
- Assignment of a string to various FixedString structs provided by `Unity.Collections` (e.g `FixedString128`)

A string can be either:
-  A string literal (e.g `"This is a string literal"`).
-  An interpolated string (using `$"This is an integer {value}` or using `string.Format`) where the string to format is also a string literal

For example, the following constructions are supported:

- Logging with a string literal:

```c#
Debug.Log("This a string literal");
```

- Logging using string interpolation:

```c#
int value = 256;
Debug.Log($"This is an integer value {value}"); 
```

Which is equivalent to using `string.Format` directly:

```c#
int value = 256;
Debug.Log(string.Format("This is an integer value {0}", value));
```

While it is possible to pass a managed `string` literal or an interpolated string directly to `Debug.Log`, it is not possible to pass a string to a user method or to use them as fields in a struct. In order to pass or store strings around, you need to use one of the `FixedString` structs provided by the `Unity.Collections` package:

```c#
int value = 256;
FixedString128 text = $"This is an integer value {value} used with FixedString128";
MyCustomLog(text);

// ...

// String can be passed as an argument to a method using a FixedString, 
// but not using directly a managed `string`:
public static void MyCustomLog(in FixedString128 log)
{
    Debug.Log(text);
}
```

Burst has limited support for string format arguments and specifiers:

```c#
int value = 256;

// Padding left: "This value `  256`
Debug.Log($"This value `{value,5}`");

// Padding right: "This value `256  `
Debug.Log($"This value `{value,-5}`");

// Hexadecimal uppercase: "This value `00FF`
Debug.Log($"This value `{value:X4}`");

// Hexadecimal lowercase: "This value `00ff`
Debug.Log($"This value `{value:x4}`");

// Decimal with leading-zero: "This value `0256`
Debug.Log($"This value `{value:D4}`");
```

What is supported currently:

- The following `Debug.Log` methods:
  - `Debug.Log(object)`
  - `Debug.LogWarning(object)`
  - `Debug.LogError(object)`
- String interpolation is working with the following caveats:
  - The string to format must be a literal.
  - Only the following `string.Format` methods are supported:
    - `string.Format(string, object)`, `string.Format(string, object, object)`, `string.Format(string, object, object, object)` and more if the .NET API provides specializations with object arguments.
    - `string.Format(string, object[])`: which can happen for a string interpolation that would contain more than 3 arguments (e.g `$"{arg1} {arg2} {arg3} {arg4} {arg5}..."`). In that case, we expect the object[] array to be of a constant size and no arguments should involve control flows (e.g `$"This is a {(cond ? arg1 : arg2)}"`)
  - Only value types.
  - Only primitive type arguments: `char`, `boolean`, `byte`, `sbyte`, `ushort`, `short`, `uint`, `int`, `ulong`, `long`, `float`, `double`.
  - All vector types e.g `int2`, `float3` are supported, except `half` vector types.
    ```c#
    var value = new float3(1.0f, 2.0f, 3.0f);
    // Logs "This value float3(1f, 2f, 3f)"
    Debug.Log($"This value `{value}`");
    ```
  - Does not support `ToString()` of structs. It will display the full name of the struct instead.
- The supported string format specifiers are only `G`, `g`, `D`, `d` and `X`, `x`, with padding and specifier. For more details:
  - See the [.NET documentation - string interpolation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated).
  - See the [.NET documentation - Standard numeric format strings](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

## Support for strings and `ProfilerMarker`

In Unity 2020.2 and above, you can call `new ProfilerMarker("MarkerName")` from Burst code:

```c#
[BurstCompile]
private static class ProfilerMarkerWrapper
{
    private static readonly ProfilerMarker StaticMarker = new ProfilerMarker("TestStaticBurst");

    [BurstCompile(CompileSynchronously = true)]
    public static int CreateAndUseProfilerMarker(int start)
    {
        using (StaticMarker.Auto())
        {
            var p = new ProfilerMarker("TestBurst");
            p.Begin();
            var result = 0;
            for (var i = start; i < start + 100000; i++)
            {
                result += i;
            }
            p.End();
            return result;
        }
    }
}
```