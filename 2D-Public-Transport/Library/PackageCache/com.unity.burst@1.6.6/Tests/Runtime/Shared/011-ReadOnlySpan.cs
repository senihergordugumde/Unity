using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Burst.Compiler.IL.Tests
{
#if UNITY_2021_2_OR_NEWER || BURST_INTERNAL
    /// <summary>
    /// Test <see cref="System.ReadOnlySpan{T}"/>.
    /// </summary>
    internal partial class ReadOnlySpan
    {
        [TestCompiler]
        public static int CreateDefault()
        {
            var span = new ReadOnlySpan<int>();

            return span.Length;
        }

        [TestCompiler]
        public static int CreateStackalloc()
        {
            ReadOnlySpan<int> span = stackalloc int[42];

            return span.Length;
        }

        [TestCompiler(42)]
        public static int CreateFromNullPointer(int size)
        {
            ReadOnlySpan<double> span;

            unsafe
            {
                span = new ReadOnlySpan<double>(null, size);
            }

            return span.Length;
        }

        [TestCompiler]
        public static unsafe double CreateFromMalloc()
        {
            double* malloc = (double*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<double>(), UnsafeUtility.AlignOf<double>(), Unity.Collections.Allocator.Persistent);
            *malloc = 42.0f;

            var span = new ReadOnlySpan<double>(malloc, 1);

            double result = span[0];

            UnsafeUtility.Free(malloc, Unity.Collections.Allocator.Persistent);

            return result;
        }

        [TestCompiler]
        public static int GetItem()
        {
            ReadOnlySpan<int> span = stackalloc int[42];
            return span[41];
        }

        [TestCompiler]
        public static int SliceFromStart()
        {
            ReadOnlySpan<int> span = stackalloc int[42];

            var newSpan = span.Slice(10);

            return newSpan[0] + newSpan.Length;
        }

        [TestCompiler]
        public static int SliceFromStartWithLength()
        {
            ReadOnlySpan<int> span = stackalloc int[42];

            var newSpan = span.Slice(10, 4);

            return newSpan[3] + newSpan.Length;
        }

        [TestCompiler]
        public static int CopyTo()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            ReadOnlySpan<int> other = stackalloc int[4];

            other.CopyTo(span);

            int result = 0;

            for (int i = 0; i < span.Length; i++)
            {
                result += span[i];
            }

            return result;
        }

        [TestCompiler]
        public static int IsEmpty() => new ReadOnlySpan<int>().IsEmpty ? 1 : 0;

        [TestCompiler]
        public static int Empty() => ReadOnlySpan<double>.Empty.Length;

        [TestCompiler]
        public static int GetEnumerator()
        {
            ReadOnlySpan<int> span = stackalloc int[42];

            int result = 0;

            var enumerator = span.GetEnumerator();

            while (enumerator.MoveNext())
            {
                result += enumerator.Current;
            }

            return result;
        }

        [TestCompiler]
        public static int OperatorEquality() => new ReadOnlySpan<double>() == ReadOnlySpan<double>.Empty ? 1 : 0;

        [TestCompiler]
        public static int OperatorInEquality() => new ReadOnlySpan<double>() != ReadOnlySpan<double>.Empty ? 1 : 0;

        [TestCompiler]
        public static int Fixed()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            ReadOnlySpan<int> readOnlySpan = span;

            unsafe
            {
                fixed (int* ptr = readOnlySpan)
                {
                    return ptr[41];
                }
            }
        }
    }
#endif
}
