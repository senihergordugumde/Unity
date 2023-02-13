using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Burst.Compiler.IL.Tests
{
    internal class ControlFlowsTryCatchFinally
    {
        [TestCompiler(-10)]
        [TestCompiler(0)]
        [TestCompiler(10)]
        public static int TryFinallySimple(int i)
        {
            try
            {
                if (i == 0) // case 0
                {
                    return 1;
                }
                else if (i > 0) // case 10
                {
                    i = i * 2;
                }
                else
                {
                    i = i * 3; // case -10
                }
            }
            finally
            {
                i = i + 1;
            }

            return i; // both case 10 and -10
        }

        static void Oof()
        {
        }

        [TestCompiler]
        public static void TryFinallyFirstBlock()
        {
            try
            {
            }
            finally
            {
                Oof();
            }
        }


        [TestCompiler(-3)]
        [TestCompiler(0)]
        [TestCompiler(3)]
        public static int TryFinallyComplex1(int i)
        {
            try
            {
                try
                {
                    if (i == 0)
                    {
                        return i - 1;
                    }

                    i += 3;
                }
                finally
                {
                    if (i == 0) // case i: -3
                    {
                        i = 1;
                    }
                    else
                    {
                        i = i * 10; // case i: 3
                    }
                }
            }
            finally
            {
                i = i * 2; // both -3 and 3
            }

            return i + 1;
        }

        [TestCompiler(-10)]
        [TestCompiler(0)] // case 0
        [TestCompiler(10)]
        public static int TryFinallyComplex2(int i)
        {
            // First block of nested try/catch
            try
            {
                try
                {
                    if (i == 0) // case 0
                    {
                        return i - 1;
                    }

                    i = i * 2;
                }
                finally
                {
                    i++;
                }
            }
            finally
            {
                i = i * 3;
            }

            // Second block of nested try/catch
            try
            {
                i = i - 2;

                try
                {
                    if (i < 0) // case -10
                    {
                        return i * 5;
                    }

                    i += 3; // case 10
                }
                finally
                {
                    i += 11;
                }
            }
            finally
            {
                i = i * 3;
            }

            return i + 1; // case 10
        }

        [TestCompiler]
        public static int TryUsingDispose()
        {
            using (var buffer = new CustomBuffer(32))
            {
                return buffer.Hash();
            }
        }

        [TestCompiler]
        public static int ForEachTryFinally()
        {
            int hashCode = 0;
            foreach (var value in new RangeEnumerable(1, 100))
            {
                hashCode = (hashCode * 397) ^ value;
            }
            return hashCode;
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_CatchConstructionNotSupported)]
        public static int TryCatch()
        {
            try
            {
                return default(int);
            }
            catch (InvalidOperationException)
            {
                return 1;
            }
        }

        private unsafe struct CustomBuffer : IDisposable
        {
            private readonly int _size;
            private byte* _buffer;

            public CustomBuffer(int size)
            {
                _size = size;
                _buffer = (byte*)UnsafeUtility.Malloc(size, 4, Allocator.Persistent);
                for (int i = 0; i < size; i++)
                {
                    _buffer[i] = (byte)(i + 1);
                }
            }

            public int Hash()
            {
                int hashCode = _size;
                for (int i = 0; i < _size; i++)
                {
                    hashCode = (hashCode * 397) ^ (byte)_buffer[i];
                }
                return hashCode;
            }

            public unsafe void Dispose()
            {
                if (_buffer != null)
                {
                    UnsafeUtility.Free(_buffer, Allocator.Persistent);
                    _buffer = (byte*) 0;
                }
            }
        }

        private struct RangeEnumerable : IEnumerable<int>
        {
            private readonly int _from;
            private readonly int _to;

            public RangeEnumerable(int from, int to)
            {
                _from = @from;
                _to = to;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator();
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<int>
            {
                private readonly int _from;
                private readonly int _to;

                public Enumerator(int from, int to)
                {
                    _from = @from;
                    _to = to;
                    Current = -1;
                }

                public void Dispose()
                {
                    // nothing to do
                }

                public bool MoveNext()
                {
                    if (Current < 0)
                    {
                        Current = _from;
                        return true;
                    }

                    int nextIndex = Current + 1;
                    if (nextIndex >= _from && nextIndex <= _to)
                    {
                        Current = nextIndex;
                        return true;
                    }
                    return false;
                }

                public void Reset()
                {
                }

                public int Current { get; private set; }

                object IEnumerator.Current => Current;
            }
        }
    }
}
