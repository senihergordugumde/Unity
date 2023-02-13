using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Burst.Compiler.IL.Tests
{
#if UNITY_2021_2_OR_NEWER || BURST_INTERNAL
    /// <summary>
    /// Test <see cref="System.Span{T}"/>.
    /// </summary>
    internal partial class Span
    {
        [TestCompiler]
        public static int CreateDefault()
        {
            var span = new Span<int>();

            return span.Length;
        }

        [TestCompiler]
        public static int CreateStackalloc()
        {
            Span<int> span = stackalloc int[42];

            return span.Length;
        }

        [TestCompiler(42)]
        public static int CreateFromNullPointer(int size)
        {
            Span<double> span;

            unsafe
            {
                span = new Span<double>(null, size);
            }

            return span.Length;
        }

        [TestCompiler]
        public static unsafe double CreateFromMalloc()
        {
            double* malloc = (double*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<double>(), UnsafeUtility.AlignOf<double>(), Unity.Collections.Allocator.Persistent);
            *malloc = 42.0f;

            Span<double> span = new Span<double>(malloc, 1);

            double result = span[0];

            UnsafeUtility.Free(malloc, Unity.Collections.Allocator.Persistent);

            return result;
        }

        [TestCompiler]
        public static int GetItem()
        {
            Span<int> span = stackalloc int[42];
            return span[41];
        }

        [TestCompiler]
        public static int SetItem()
        {
            Span<int> span = stackalloc int[42];
            span[41] = 13;
            return span[41];
        }

        [TestCompiler]
        public static int Clear()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            span.Clear();

            int result = 0;

            for (int i = 0; i < span.Length; i++)
            {
                result += span[i];
            }

            return result;
        }

        [TestCompiler]
        public static int SliceFromStart()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            var newSpan = span.Slice(10);

            return newSpan[0] + newSpan.Length;
        }

        [TestCompiler]
        public static int SliceFromStartWithLength()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

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

            Span<int> other = stackalloc int[4];

            for (int i = 0; i < other.Length; i++)
            {
                other[i] = -i - 1;
            }

            other.CopyTo(span);

            int result = 0;

            for (int i = 0; i < span.Length; i++)
            {
                result += span[i];
            }

            return result;
        }

        [TestCompiler]
        public static int Fill()
        {
            Span<int> span = stackalloc int[42];

            span.Fill(123);

            int result = 0;

            for (int i = 0; i < span.Length; i++)
            {
                result += span[i];
            }

            return result;
        }

        [TestCompiler]
        public static int IsEmpty() => new Span<int>().IsEmpty ? 1 : 0;

        [TestCompiler]
        public static int Empty() => Span<double>.Empty.Length;

        [TestCompiler]
        public static int GetEnumerator()
        {
            Span<int> span = stackalloc int[42];

            int result = 0;

            var enumerator = span.GetEnumerator();

            while (enumerator.MoveNext())
            {
                result += enumerator.Current;
            }

            return result;
        }

        [TestCompiler]
        public static int OperatorEquality() => new Span<double>() == Span<double>.Empty ? 1 : 0;

        [TestCompiler]
        public static int OperatorInEquality() => new Span<double>() != Span<double>.Empty ? 1 : 0;

        [TestCompiler]
        public static int OperatorImplicit()
        {
            ReadOnlySpan<double> span = new Span<double>();

            return span.Length;
        }

        [TestCompiler]
        public static int Fixed()
        {
            Span<int> span = stackalloc int[42];

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            unsafe
            {
                fixed (int* ptr = span)
                {
                    *ptr = 42;
                    return ptr[41];
                }
            }
        }
    }
#endif
}
