# Unity.Burst.Intrinsics

Burst provides low level close-to-the-metal intrinsics via the `Unity.Burst.Intrinsics` namespace.

## Common

The `Unity.Burst.Intrinsics.Common` intrinsics are for functionality that is shared across the hardware targets that Burst supports.

### Pause

The `Unity.Burst.Intrinsics.Common.Pause` is an intrinsic that provides a hint that the current thread should pause. It maps to `pause` on x86, and `yield` on ARM.

It is primarily used to stop spin locks over contending on an atomic access - reduce contention and power on that section of code.

### Prefetch

The `Unity.Burst.Intrinsics.Common.Prefetch` is an **experimental** intrinsic that provides a hint that the memory location should be prefetched into the cache.

The intrinsic is **experimental** and so guarded by the `UNITY_BURST_EXPERIMENTAL_PREFETCH_INTRINSIC` preprocessor define.

### umul128

The `Unity.Burst.Intrinsics.Common.umul128` is an intrinsic that enables users to access 128-bit unsigned multiplication. These multiplies have become increasingly prevalent in hashing functions. It maps 1:1 with hardware instructions on x86 and ARM targets.

## Processor specific SIMD extensions

Burst exposes all Intel SIMD intrinsics from SSE up to and including AVX2
by means of the `Unity.Burst.Intrinsics.X86` family of nested classes, 
Arm Neon intrinsics for Armv7 and Armv8, and Armv8.2 Neon intrinsics (RDMA, crypto, dotprod) by means of the `Unity.Burst.Intrinsics.Arm.Neon` class.
These are intended to be statically imported as they contain plain static functions:

```c#
using static Unity.Burst.Intrinsics.X86;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse3;
using static Unity.Burst.Intrinsics.X86.Ssse3;
using static Unity.Burst.Intrinsics.X86.Sse4_1;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Burst.Intrinsics.X86.Popcnt;
using static Unity.Burst.Intrinsics.X86.Avx;
using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Fma;
using static Unity.Burst.Intrinsics.X86.F16C;
using static Unity.Burst.Intrinsics.X86.Bmi1;
using static Unity.Burst.Intrinsics.X86.Bmi2;
using static Unity.Burst.Intrinsics.Arm.Neon;
```

Each feature level above provides a compile-time check to test if the feature
level is present at compile-time:

```c#
if (IsAvx2Supported)
{
    // Code path for AVX2 instructions
}
else if (IsSse42Supported)
{
    // Code path for SSE4.2 instructions
}
else if (IsNeonArmv82FeaturesSupported)
{
    // Code path for Armv8.2 Neon instructions
}
else if (IsNeonSupported)
{
    // Code path for Arm Neon instructions
}
else
{
    // Fallback path for everything else
}
```

Later feature levels implicitly include the previous ones, so tests must be
organized from most recent to least recent. Burst will emit compile-time errors
if there are uses of intrinsics that are not part of the current compilation
target which are also not bracketed with a feature level test, helping you to
narrow in on what needs to be put inside a feature test.

Note that when running in .NET, Mono or IL2CPP without Burst enabled, all the `IsXXXSupported` properties will return `false`.
However, if you skip the test you can still run a reference version of most
intrinsics in Mono (exceptions listed below), which can be helpful if you need to use the managed
debugger. However, please note that the reference implementations are very slow
and only intended for managed debugging.

> Please note that there is no reference managed implementation of Arm Neon intrinsics. This means that you cannot use the technique mentioned in the previous paragraph to step through the intrinsics in Mono.
> Note that the FMA intrinsics that operate on doubles do not have a software fallback because of the inherit complexity in emulating fused 64-bit floating point math.

Intrinsic usage is relatively straightforward and is based on the types `v64` (Arm only), `v128`
and `v256`, which represent a 64-bit, 128-bit or 256-bit vector respectively. For example,
given a `NativeArray<float>` and a `Lut` lookup table of v128 shuffle masks,
a code fragment like this performs lane left packing, demonstrating the use
of vector load/store reinterpretation and direct intrinsic calls:

```c#
v128 a = Input.ReinterpretLoad<v128>(i);
v128 mask = cmplt_ps(a, Limit);
int m = movemask_ps(a);
v128 packed = shuffle_epi8(a, Lut[m]);
Output.ReinterpretStore(outputIndex, packed);
outputIndex += popcnt_u32((uint)m);
```

The Intel intrinsics API mirrors the [C/C++ Intel instrinsics API](https://software.intel.com/sites/landingpage/IntrinsicsGuide/), with a few mostly
mechanical differences:

* All 128-bit vector types (`__m128`, `__m128i` and `__m128d`) have been collapsed into `v128`
* All 256-bit vector types (`__m256`, `__m256i` and `__m256d`) have been collapsed into `v256`
* All `_mm` prefixes on instructions and macros have been dropped, as C# has namespaces
* All bitfield constants (for e.g. rounding mode selection) have been replaced with C# bitflag enum values

The Arm Neon intrinsics API mirrors the [Arm C Language Extensions](https://developer.arm.com/architectures/instruction-sets/simd-isas/neon/intrinsics), 
with the following differences:

* All vector types have been collapsed into `v64` and `v128`, becoming "typeless". It means that you must make sure that the vector type actually contains expected element type and count when calling an API.
* The *x2, *x3, *x4 vector types are not supported.
* poly* types are not supported.
* reinterpret* functions are not supported (they are not needed because of the usage of `v64` and `v128` vector types).
* Intrinsic usage is only supported on Armv8 (64-bit) hardware.

# `DllImport` and internal calls

Burst supports calling native functions via `[DllImport]`:

```c#
[DllImport("MyNativeLibrary")]
public static extern int Foo(int arg);
```

as well as "internal" calls implemented inside Unity:

```c#
// In UnityEngine.Mathf
[MethodImpl(MethodImplOptions.InternalCall)]
public static extern int ClosestPowerOfTwo(int value);
```

For all `DllImport` and internal calls, only types in the following list can be used as
parameter or return types:

* Primitive and intrinsic types
  * `byte`
  * `ushort`
  * `uint`
  * `ulong`
  * `sbyte`
  * `short`
  * `int`
  * `long`
  * `float`
  * `double`
  * `System.IntPtr`
  * `System.UIntPtr`
  * `Unity.Burst.Intrinsics.v64`
  * `Unity.Burst.Intrinsics.v128`
  * `Unity.Burst.Intrinsics.v256`
* Pointers and references
  * `sometype*` - Pointer to any of the other types in this list
  * `ref sometype` - Reference to any of the other types in this list
* "Handle" structs
  * `unsafe struct MyStruct { void* Ptr; }` - Struct containing a single pointer field
  * `unsafe struct MyStruct { int Value; }` - Struct containing a single integer field

> Note that passing structs by value is not supported; you need to pass them through a pointer or reference.
The only exception is that "handle" structs are supported - these are structs that contain a 
single field of pointer or integer type.

[Known issues with `DllImport`](KnownIssues.md#known-issues-with-dllimport)
