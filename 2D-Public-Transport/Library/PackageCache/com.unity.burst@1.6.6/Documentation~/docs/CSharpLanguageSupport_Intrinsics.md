# Intrinsics

## `System.Math`

Burst provides an intrinsic for all methods declared by `System.Math` except for the following methods that are not supported:
 - `double IEEERemainder(double x, double y)` 
 - `Round(double value, int digits)`

## `System.IntPtr`

Burst supports all methods of `System.IntPtr`/`System.UIntPtr`, including the static fields `IntPtr.Zero` and `IntPtr.Size`

## `System.Threading.Interlocked`

Burst supports atomic memory intrinsics for all methods provided by `System.Threading.Interlocked` (e.g `Interlocked.Increment`...etc.)

Care must be taken when using the interlocked methods that the source location being atomically accessed is _naturally aligned_ - e.g. the alignment of the pointer is a multiple of the pointed-to-type.

For example:

```c#
[StructLayout(LayoutKind.Explicit)]
struct Foo
{
    [FieldOffset(0)] public long a;
    [FieldOffset(5)] public long b;

    public long AtomicReadAndAdd()
    {
        return Interlocked.Read(ref a) + Interlocked.Read(ref b);
    }
}
```

Let's assume that the pointer to the struct `Foo` has an alignment of 8 - the natural alignment of a `long` value. The `Interlocked.Read` of `a` would be successful because it lies on a _naturally aligned_ address, but `b` would not. Undefined behaviour will occur at the load of `b` as a result.

## `System.Threading.Thread`

Burst supports the `MemoryBarrier` method of `System.Threading.Thread`.

## `System.Threading.Volatile`

Burst supports the non-generic variants of `Read` and `Write` provided by `System.Threading.Volatile`.