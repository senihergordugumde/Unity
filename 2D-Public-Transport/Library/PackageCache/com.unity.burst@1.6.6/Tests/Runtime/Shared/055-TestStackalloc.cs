namespace Burst.Compiler.IL.Tests.Shared
{
    internal class TestStackalloc
    {
        [TestCompiler]
        public static unsafe int Stackalloc1ByteWithInitializer()
        {
            var value = stackalloc byte[1] { 0xA4 };
            return value[0];
        }

        [TestCompiler]
        public static unsafe int Stackalloc16BytesWithInitializer()
        {
            // Roslyn generates quite different IL when the number of bytes is larger than 8.
            var value = stackalloc byte[16] { 0xA4, 0xA1, 0x20, 0xA5, 0x80, 0x17, 0xF6, 0x4F, 0xBD, 0x18, 0x16, 0x73, 0x43, 0xC5, 0xAF, 0x16 };
            return value[9];
        }

        [TestCompiler]
        public static unsafe int Stackalloc16IntsWithInitializer()
        {
            var value = stackalloc int[16] { 0xA4, 0xA1, 0x20, 0xA5, 0x80, 0x17, 0xF6, 0x4F, 0xBD, 0x18, 0x16, 0x73, 0x43, 0xC5, 0xAF, 0x16 };
            return value[9];
        }

        [TestCompiler(1)]
        public static unsafe int StackallocInBranch(int takeBranch)
        {
            int* array = null;

            if (takeBranch != 0)
            {
                int* elem = stackalloc int[1];
                array = elem;
            }

            if (takeBranch != 0)
            {
                int* elem = stackalloc int[1];

                if (array == elem)
                {
                    return -1;
                }
            }

            return 0;
        }

        [TestCompiler(4)]
        public static unsafe int StackallocInLoop(int iterations)
        {
            int** array = stackalloc int*[iterations];

            for (int i = 0; i < iterations; i++)
            {
                int* elem = stackalloc int[1];
                array[i] = elem;
            }

            for (int i = 0; i < iterations; i++)
            {
                for (int k = i + 1; k < iterations; k++)
                {
                    // Make sure all the stack allocations within the loop are unique addresses.
                    if (array[i] == array[k])
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }

        // These tests require 2020.2 because that enabled C# 8.0.
#if BURST_TESTS_ONLY || UNITY_2020_2_OR_NEWER
        [TestCompiler]
        public static unsafe int StackallocWithUnmanagedConstructedType()
        {
            var value = stackalloc[]
            {
                new Point<int> { X = 1, Y = 2 },
                new Point<int> { X = 42, Y = 5 },
                new Point<int> { X = 3, Y = -1 },
            };
            return value[1].X;
        }

        private struct Point<T>
        {
            public T X;
            public T Y;
        }
#endif

#if UNITY_2021_2_OR_NEWER || BURST_INTERNAL
        [TestCompiler]
        public static int StackallocInNestedExpression()
        {
            return StackallocInNestedExpressionHelper(stackalloc[] { 2, 4, 6, 8 });
        }

        private static int StackallocInNestedExpressionHelper(System.Span<int> span)
        {
            return span[2];
        }
#endif
    }
}
