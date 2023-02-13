# Optimization Guidelines

# Loop Vectorization

Loop vectorization is one of the ways that Burst improves performance. Let's say you have code like this:

``` c#
[MethodImpl(MethodImplOptions.NoInlining)]
private static unsafe void Bar([NoAlias] int* a, [NoAlias] int* b, int count)
{
    for (var i = 0; i < count; i++)
    {
        a[i] += b[i];
    }
}

public static unsafe void Foo(int count)
{
    var a = stackalloc int[count];
    var b = stackalloc int[count];

    Bar(a, b, count);
}
```

The compiler is able to convert that scalar loop in `Bar` into a vectorized loop. Instead of looping over a single value at a time,
the compiler generates code that loops over multiple values at the same time, producing faster code essentially for free. Here is the 
`x64` assembly generated for `AVX2` for the loop in `Bar` above:

```
.LBB1_4:
    vmovdqu    ymm0, ymmword ptr [rdx + 4*rax]
    vmovdqu    ymm1, ymmword ptr [rdx + 4*rax + 32]
    vmovdqu    ymm2, ymmword ptr [rdx + 4*rax + 64]
    vmovdqu    ymm3, ymmword ptr [rdx + 4*rax + 96]
    vpaddd     ymm0, ymm0, ymmword ptr [rcx + 4*rax]
    vpaddd     ymm1, ymm1, ymmword ptr [rcx + 4*rax + 32]
    vpaddd     ymm2, ymm2, ymmword ptr [rcx + 4*rax + 64]
    vpaddd     ymm3, ymm3, ymmword ptr [rcx + 4*rax + 96]
    vmovdqu    ymmword ptr [rcx + 4*rax], ymm0
    vmovdqu    ymmword ptr [rcx + 4*rax + 32], ymm1
    vmovdqu    ymmword ptr [rcx + 4*rax + 64], ymm2
    vmovdqu    ymmword ptr [rcx + 4*rax + 96], ymm3
    add        rax, 32
    cmp        r8, rax
    jne        .LBB1_4
```

As can be seen above, the loop has been unrolled and vectorized so that it is 4 `vpaddd` instructions, each calculating 8 integer additions,
for a total of **32 integer additions per loop iteration**.

This is great! However, loop vectorization is notoriously brittle. As an example, let's introduce a seemingly innocuous branch like this:

``` c#
[MethodImpl(MethodImplOptions.NoInlining)]
private static unsafe void Bar([NoAlias] int* a, [NoAlias] int* b, int count)
{
    for (var i = 0; i < count; i++)
    {
        if (a[i] > b[i])
        {
            break;
        }

        a[i] += b[i];
    }
}
```

Now the assembly changes to this:

```
.LBB1_3:
    mov        r9d, dword ptr [rcx + 4*r10]
    mov        eax, dword ptr [rdx + 4*r10]
    cmp        r9d, eax
    jg        .LBB1_4
    add        eax, r9d
    mov        dword ptr [rcx + 4*r10], eax
    inc        r10
    cmp        r8, r10
    jne        .LBB1_3
```

This loop is completely scalar and only has 1 integer addition per loop iteration. This is not good! In this simple case,
an experienced developer would probably spot that adding the branch will break auto-vectorization. But in more complex real-life code
it can be difficult to spot.

To help with this problem, Burst includes, at present, **experimental** intrinsics (`Loop.ExpectVectorized()` and `Loop.ExpectNotVectorized()`) to express loop vectorization
assumptions, and have them validated at compile-time. For example, we can change the original `Bar` implementation to:

``` c#
[MethodImpl(MethodImplOptions.NoInlining)]
private static unsafe void Bar([NoAlias] int* a, [NoAlias] int* b, int count)
{
    for (var i = 0; i < count; i++)
    {
        Unity.Burst.CompilerServices.Loop.ExpectVectorized();

        a[i] += b[i];
    }
}
```

Burst will now validate, at compile-time, that the loop has indeed been vectorized. If the loop is not vectorized, Burst will
emit a compiler error. For example, if we do this:

``` c#
[MethodImpl(MethodImplOptions.NoInlining)]
private static unsafe void Bar([NoAlias] int* a, [NoAlias] int* b, int count)
{
    for (var i = 0; i < count; i++)
    {
        Unity.Burst.CompilerServices.Loop.ExpectVectorized();

        if (a[i] > b[i])
        {
            break;
        }

        a[i] += b[i];
    }
}
```

then Burst will emit the following error at compile-time:

```
LoopIntrinsics.cs(6,9): Burst error BC1321: The loop is not vectorized where it was expected that it is vectorized.
```

As these intrinsics are **experimental**, they need to be enabled with the `UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS` preprocessor define.

> Note that these loop intrinsics should not be used inside `if` statements. Burst does not currently prevent this from happening, but in a future release this will be a compile-time error.

# Compiler Options

When compiling a job, you can change the behavior of the compiler:

- Using a different accuracy for the math functions (sin, cos...)
- Allowing the compiler to re-arrange the floating point calculations by relaxing the order of the math computations.
- Forcing a synchronous compilation of the Job (only for the Editor/JIT case)
- Using internal compiler options (not yet detailed)

These flags can be set through the `[BurstCompile]` attribute, for example `[BurstCompile(FloatPrecision.Med, FloatMode.Fast)]`

## FloatPrecision

The accuracy is defined by the following enumeration:

``` c#
    public enum FloatPrecision
    {
        /// <summary>
        /// Use the default target floating point precision - <see cref="FloatPrecision.Medium"/>.
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Compute with an accuracy of 1 ULP - highly accurate, but increased runtime as a result, should not be required for most purposes.
        /// </summary>
        High = 1,
        /// <summary>
        /// Compute with an accuracy of 3.5 ULP - considered acceptable accuracy for most tasks.
        /// </summary>
        Medium = 2,
        /// <summary>
        /// Compute with an accuracy lower than or equal to <see cref="FloatPrecision.Medium"/>, with some range restrictions (defined per function).
        /// </summary>
        Low = 3,
    }
```

Currently, the implementation is only providing the following accuracy:

- `FloatPrecision.Standard` is equivalent to `FloatPrecision.Medium` providing an accuracy of 3.5 ULP. This is the **default value**.
- `FloatPrecision.High` provides an accuracy of 1.0 ULP.
- `FloatPrecision.Medium` provides an accuracy of 3.5 ULP.
- `FloatPrecision.Low` has an accuracy defined per function, and functions may specify a restricted range of valid inputs.

Using the `FloatPrecision.Standard` accuracy should be largely enough for most games.

An ULP (unit in the last place or unit of least precision) is the spacing between floating-point numbers, i.e., the value the least significant digit represents if it is 1.

Note: The `FloatPrecision` Enum was named `Accuracy` in early versions of the Burst API.

### FloatPrecision.Low

The following table describes the precision and range restrictions for using the `FloatPrecision.Low` mode. Any function **not** described in the table will inherit the ULP requirement from `FloatPrecision.Medium`.

<br/>
<table>
  <tr><th>Function</th><th>Precision</th><th>Range</th></tr>
  <tr><td>Unity.Mathematics.math.sin(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.cos(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.exp(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.exp2(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.exp10(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.log(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.log2(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.log10(x)</td><td>350.0 ULP</td><td></td></tr>
  <tr><td>Unity.Mathematics.math.pow(x, y)</td><td>350.0 ULP</td><td>Negative `x` to the power of a fractional `y` are not supported.</td></tr>
</table>

## Compiler floating point math mode

The compiler floating point math mode is defined by the following enumeration:

```c#
    /// <summary>
    /// Represents the floating point optimization mode for compilation.
    /// </summary>
    public enum FloatMode
    {
        /// <summary>
        /// Use the default target floating point mode - <see cref="FloatMode.Strict"/>.
        /// </summary>
        Default = 0,
        /// <summary>
        /// No floating point optimizations are performed.
        /// </summary>
        Strict = 1,
        /// <summary>
        /// Reserved for future.
        /// </summary>
        Deterministic = 2,
        /// <summary>
        /// Allows algebraically equivalent optimizations (which can alter the results of calculations), it implies :
        /// <para/> optimizations can assume results and arguments contain no NaNs or +/- Infinity and treat sign of zero as insignificant.
        /// <para/> optimizations can use reciprocals - 1/x * y  , instead of  y/x.
        /// <para/> optimizations can use fused instructions, e.g. madd.
        /// </summary>
        Fast = 3,
    }
```

- `FloatMode.Default` is defaulting to `FloatMode.Strict`
- `FloatMode.Strict`: The compiler is not performing any re-arrangement of the calculation and will be careful at respecting special floating point values (denormals, NaN...). This is the **default value**.
- `FloatMode.Fast`: The compiler can perform instruction re-arrangement and/or using dedicated/less precise hardware SIMD instructions.
- `FloatMode.Deterministic`: Reserved for future, when Burst will provide support for deterministic mode

Typically, some hardware can support Multiply and Add (e.g mad `a * b + c`) into a single instruction. These optimizations can be allowed by using the Fast calculation.
The reordering of these instructions can lead to a lower accuracy.

The `FloatMode.Fast` compiler floating point math mode can be used for many scenarios where the exact order of the calculation and the uniform handling of NaN values are not strictly required.

# AssumeRange Attribute

Being able to tell the compiler that an integer lies within a certain range can open up optimization opportunities. The `AssumeRange` attribute allows users to tell the compiler that a given scalar-integer lies within a certain constrained range:

```c#
[return:AssumeRange(0u, 13u)]
static uint WithConstrainedRange([AssumeRange(0, 26)] int x)
{
    return (uint)x / 2u;
}
```

The above code makes two promises to the compiler:

- That the variable `x` is in the closed-interval range `[0..26]`, or more plainly that `x >= 0 && x <= 26`.
- That the return value from `WithConstrainedRange` is in the closed-interval range `[0..13]`, or more plainly that `x >= 0 && x <= 13`.

These assumptions are fed into the optimizer and allow for better codegen as a result. There are some restrictions:

- You can **only** place these on scalar-integer (signed or unsigned) types.
- The type of the range arguments **must match** the type being attributed.

We've also added in some deductions for the `.Length` property of `NativeArray` and `NativeSlice` to tell the optimizer that these always return non-negative integers.

```c#
static bool IsLengthNegative(NativeArray<float> na)
{
    // The compiler will always replace this with the constant false!
    return na.Length < 0;
}
```

Let's assume you have your own container:

```c#
struct MyContainer
{
    public int Length;
    
    // Some other data...
}
```

And you wanted to tell Burst that `Length` was always a positive integer. You would do that like so:

```c#
struct MyContainer
{
    private int _length;

    [return: AssumeRange(0, int.MaxValue)]
    private int LengthGetter()
    {
        return _length;
    }

    public int Length
    {
        get => LengthGetter();
        set => _length = value;
    }

    // Some other data...
}
```

# Hint Intrinsics

Burst has some `Hint` intrinsics that provide a way for developers to tell the optimizer additional information that could aid in optimizations:

- `Unity.Burst.CompilerServices.Hint.Likely` lets developers tell Burst that a boolean condition is likely to be true.
- `Unity.Burst.CompilerServices.Hint.Unlikely` lets developers tell Burst that a boolean condition is unlikely to be true.
- `Unity.Burst.CompilerServices.Hint.Assume` lets developers tell Burst that a boolean condition can be assumed to be true.

The likely intrinsic is most useful to tell Burst which branch condition has a high probability of being taken, and thus the optimizer can focus on the branch in question for optimization purposes:

```c#
if (Unity.Burst.CompilerServices.Hint.Likely(b))
{
    // Any code in here will be optimized by Burst with the assumption that we'll probably get here!
}
else
{
    // Whereas the code in here will be kept out of the way of the optimizer.
}
```

Conversely, the unlikely intrinsic tells the compiler the opposite - the condition is very unlikely to be true, and it should optimize against it:

```c#
if (Unity.Burst.CompilerServices.Hint.Unlikely(b))
{
    // Whereas the code in here will be kept out of the way of the optimizer.
}
else
{
    // Any code in here will be optimized by Burst with the assumption that we'll probably get here!
}
```

These two intrinsics ensure that the code most likely to be hit will be placed after the branching condition in the binary, meaning that it will have a very high probability of being in the instruction cache. Also, the compiler can hoist code out of the likely branch if profitable, spend extra time optimizing the likely branch, and also not spend as much time looking at the unlikely code - since the developer has told the compiler it probably won't be hit.

A classic example of an unlikely branch is to check if result of an allocation is valid - the allocation will be valid nearly _all the time_, and so you want the code to be fast with that assumption, but you do need some sort of error case to fall back to.

The assume intrinsic is powerful and dangerous - telling the compiler that a condition **is always** true:

```c#
Unity.Burst.CompilerServices.Hint.Assume(b);

if (b)
{
    // The compiler has been told that b is always true, so this branch will always be taken.
}
else
{
    // Any code in here will be removed from the program because b is always true!
}
```

The power of the assume intrinsic is that it allows you to arbitrarily tell the compiler that something is true. A developer could tell the compiler to assume that a loop end is always a multiple of 16, meaning that it can provide perfect vectorization without any scalar spilling for that loop. A developer could tell the compiler that a value isn't `NaN`, is negative, etc - the sky is really the limit here.

The danger with the intrinsic though is that the compiler will assume the value is true **without checking that it really was true** - you as the developer have promised to the compiler that it must be true, and Burst is a trusting compiler - it entrusts that the promise is kept! As a result, this intrinsic should be one of the last tools left on the shelf - it is useful and powerful, but care must be taken.

# Constant Intrinsic

Burst has an intrinsic `Unity.Burst.CompilerServices.Constant.IsConstantExpression` that will return true if a given expression is known to be constant at compile-time:

```c#
using static Unity.Burst.CompilerServices.Constant;

var somethingWhichWillBeConstantFolded = math.pow(42.0f, 42.0f);

if (IsConstantExpression(somethingWhichWillBeConstantFolded))
{
    // The compiler knows that somethingWhichWillBeConstantFolded is a compile-time constant!
}
```

This can be useful to check that some complex expression that you want to be _certain_ is constant folded away with Burst is always constant folded. You could even use this to have some special case optimizations for a known constant value, for example, let's say we wanted to implement our own `pow`-like function for integer powers:

```c#
using static Unity.Burst.CompilerServices.Constant;

public static float MyAwesomePow(float f, int i)
{
    if (IsConstantExpression(i) && (2 == i))
    {
        return f * f;
    }
    else
    {
        return math.pow(f, (float)i);
    }
}
```

Using the `IsConstantExpression` check above will mean that the branch will always be removed by the compiler if `i` is not constant, because the if condition would be false. This means that if `i` is constant and is equal to 2, we'd use a more optimal simple multiply instead.

> Note that constant folding will _only_ take place during optimizations, so if you have disabled optimizations the intrinsic will return false.

# `Unity.Mathematics`

The `Unity.Mathematics` provides vector types (`float4`, `float3`...) that are directly mapped to hardware SIMD registers.

Also, many functions from the `math` type are also mapped directly to hardware SIMD instructions.

> Note that currently, for an optimal usage of this library, it is recommended to use SIMD 4 wide types (`float4`, `int4`, `bool4`...)

# Generic Jobs

As described in [AOT vs JIT](QuickStart.md#just-in-time-jit-vs-ahead-of-time-aot-compilation), there are currently two modes Burst will compile a Job:

- When in the Editor, it will compile the Job when it is scheduled (sometimes called JIT mode).
- When building a Standalone Player, it will compile the Job as part of the build player (AOT mode).

If the Job is a concrete type (not using generics), the Job will be compiled correctly in both modes.

In case of a generic Job, it can behave more unexpectedly.

While Burst supports generics, it has limited support for using generic Jobs or Function pointers. You could notice that a job scheduled at Editor time is running at full speed with Burst but not when used in a Standalone player. It is usually a problem related to generic Jobs.

A generic Job can be defined like this:

```c#
// Direct Generic Job
[BurstCompile]
struct MyGenericJob<TData> : IJob where TData : struct { 
    public void Execute() { ... }
}
```

or can be nested:

```c#
// Nested Generic Job
public class MyGenericSystem<TData> where TData : struct {
    [BurstCompile]
    struct MyGenericJob  : IJob { 
        public void Execute() { ... }
    }

    public void Run()
    {
        var myJob = new MyGenericJob(); // implicitly MyGenericSystem<TData>.MyGenericJob
        myJob.Schedule();    
    }
}
```

When the previous Jobs are being used like:

```c#
// Direct Generic Job
var myJob = new MyGenericJob<int>();
myJob.Schedule();

// Nested Generic Job
var myJobSystem = new MyGenericSystem<float>();
myJobSystem.Run();
```

In both cases in a standalone-player build, the Burst compiler will be able to detect that it has to compile `MyGenericJob<int>` and `MyGenericJob<float>` because the generic jobs (or the type surrounding it for the nested job) are used with fully resolved generic arguments (`int` and `float`).

But if these jobs are used indirectly through a generic parameter, the Burst compiler won't be able to detect the Jobs it has to compile at standalone-player build time:

```c#
public static void GenericJobSchedule<TData>() where TData: struct {
    // Generic argument: Generic Parameter TData
    // This Job won't be detected by the Burst Compiler at standalone-player build time.
    var job = new MyGenericJob<TData>();
    job.Schedule();
}

// The implicit MyGenericJob<int> will run at Editor time in full Burst speed
// but won't be detected at standalone-player build time.
GenericJobSchedule<int>();
```

Same restriction applies when declaring the Job in the context of generic parameter coming from a type:

```c#
// Generic Parameter TData
public class SuperJobSystem<TData>
{
    // Generic argument: Generic Parameter TData
    // This Job won't be detected by the Burst Compiler at standalone-player build time.
    public MyGenericJob<TData> MyJob;
}
```

> In summary, if you are using generic jobs, they need to be used directly with fully-resolved generic arguments (e.g `int`, `MyOtherStruct`), but can't be used with a generic parameter indirection (e.g `MyGenericJob<TContext>`).

Regarding function pointers, they are more restricted as you can't use a generic delegate through a function pointer with Burst:

```c#
public delegate void MyGenericDelegate<T>(ref TData data) where TData: struct;

var myGenericDelegate = new MyGenericDelegate<int>(MyIntDelegateImpl);
// Will fail to compile this function pointer.
var myGenericFunctionPointer = BurstCompiler.CompileFunctionPointer<MyGenericDelegate<int>>(myGenericDelegate);
```

This limitation is due to a limitation of the .NET runtime to interop with such delegates.

# SkipLocalsInit Attribute

In C# all local variables are initialized to zero by default. This is a great feature because it means an entire class of bugs surrounding undefined data disappears. But this can come at some cost to runtime performance, because initializing this data to zero is not free:

```c#
static unsafe int DoSomethingWithLUT(int* data);

static unsafe int DoSomething(int size)
{
    int* data = stackalloc int[size];

    // Initialize every field of data to be an incrementing set of values.
    for (int i = 0; i < size; i++)
    {
        data[i] = i;
    }

    // Use the data elsewhere.
    return DoSomethingWithLUT(data);
}
```

The X86 assembly for this is:

```C
        push    rbp
        .seh_pushreg rbp
        push    rsi
        .seh_pushreg rsi
        push    rdi
        .seh_pushreg rdi
        mov     rbp, rsp
        .seh_setframe rbp, 0
        .seh_endprologue
        mov     edi, ecx
        lea     r8d, [4*rdi]
        lea     rax, [r8 + 15]
        and     rax, -16
        movabs  r11, offset __chkstk
        call    r11
        sub     rsp, rax
        mov     rsi, rsp
        sub     rsp, 32
        movabs  rax, offset burst.memset.inline.X64_SSE4.i32@@32
        mov     rcx, rsi
        xor     edx, edx
        xor     r9d, r9d
        call    rax
        add     rsp, 32
        test    edi, edi
        jle     .LBB0_7
        mov     eax, edi
        cmp     edi, 8
        jae     .LBB0_3
        xor     ecx, ecx
        jmp     .LBB0_6
.LBB0_3:
        mov     ecx, eax
        and     ecx, -8
        movabs  rdx, offset __xmm@00000003000000020000000100000000
        movdqa  xmm0, xmmword ptr [rdx]
        mov     rdx, rsi
        add     rdx, 16
        movabs  rdi, offset __xmm@00000004000000040000000400000004
        movdqa  xmm1, xmmword ptr [rdi]
        movabs  rdi, offset __xmm@00000008000000080000000800000008
        movdqa  xmm2, xmmword ptr [rdi]
        mov     rdi, rcx
        .p2align        4, 0x90
.LBB0_4:
        movdqa  xmm3, xmm0
        paddd   xmm3, xmm1
        movdqu  xmmword ptr [rdx - 16], xmm0
        movdqu  xmmword ptr [rdx], xmm3
        paddd   xmm0, xmm2
        add     rdx, 32
        add     rdi, -8
        jne     .LBB0_4
        cmp     rcx, rax
        je      .LBB0_7
        .p2align        4, 0x90
.LBB0_6:
        mov     dword ptr [rsi + 4*rcx], ecx
        inc     rcx
        cmp     rax, rcx
        jne     .LBB0_6
.LBB0_7:
        sub     rsp, 32
        movabs  rax, offset "DoSomethingWithLUT"
        mov     rcx, rsi
        call    rax
        nop
        mov     rsp, rbp
        pop     rdi
        pop     rsi
        pop     rbp
        ret
```

But the important bit to note is the `movabs  rax, offset burst.memset.inline.X64_SSE4.i32@@32` line - we've had to inject a memset to zero out the data. In the above example the developer _knows_ that the array will be entirely initialized in the following loop, but the compiler doesn't know that. To fix this exact sort of problem, there is a Burst attribute `Unity.Burst.CompilerServices.SkipLocalsInitAttribute` that can be placed on methods to tell the compiler that any stack allocations within do not have to be initialized to zero. Let's see that in action:

```c#
using Unity.Burst.CompilerServices;

static unsafe int DoSomethingWithLUT(int* data);

[SkipLocalsInit]
static unsafe int DoSomething(int size)
{
    int* data = stackalloc int[size];

    // Initialize every field of data to be an incrementing set of values.
    for (int i = 0; i < size; i++)
    {
        data[i] = i;
    }

    // Use the data elsewhere.
    return DoSomethingWithLUT(data);
}
```

And the assembly after adding the `[SkipLocalsInit]` on the method is:

```c
        push    rbp
        .seh_pushreg rbp
        mov     rbp, rsp
        .seh_setframe rbp, 0
        .seh_endprologue
        mov     edx, ecx
        lea     eax, [4*rdx]
        add     rax, 15
        and     rax, -16
        movabs  r11, offset __chkstk
        call    r11
        sub     rsp, rax
        mov     rcx, rsp
        test    edx, edx
        jle     .LBB0_7
        mov     r8d, edx
        cmp     edx, 8
        jae     .LBB0_3
        xor     r10d, r10d
        jmp     .LBB0_6
.LBB0_3:
        mov     r10d, r8d
        and     r10d, -8
        movabs  rax, offset __xmm@00000003000000020000000100000000
        movdqa  xmm0, xmmword ptr [rax]
        mov     rax, rcx
        add     rax, 16
        movabs  rdx, offset __xmm@00000004000000040000000400000004
        movdqa  xmm1, xmmword ptr [rdx]
        movabs  rdx, offset __xmm@00000008000000080000000800000008
        movdqa  xmm2, xmmword ptr [rdx]
        mov     r9, r10
        .p2align        4, 0x90
.LBB0_4:
        movdqa  xmm3, xmm0
        paddd   xmm3, xmm1
        movdqu  xmmword ptr [rax - 16], xmm0
        movdqu  xmmword ptr [rax], xmm3
        paddd   xmm0, xmm2
        add     rax, 32
        add     r9, -8
        jne     .LBB0_4
        cmp     r10, r8
        je      .LBB0_7
        .p2align        4, 0x90
.LBB0_6:
        mov     dword ptr [rcx + 4*r10], r10d
        inc     r10
        cmp     r8, r10
        jne     .LBB0_6
.LBB0_7:
        sub     rsp, 32
        movabs  rax, offset "DoSomethingWithLUT"
        call    rax
        nop
        mov     rsp, rbp
        pop     rbp
        ret
```

And note the call to memset is gone - because the developer has promised the compiler that it is fine. Note that this is a power user feature for experienced developers - developers that are _certain_ they won't run into undefined behaviour bugs as a result of this change.
