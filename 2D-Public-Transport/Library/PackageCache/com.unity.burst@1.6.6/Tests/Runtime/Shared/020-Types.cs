using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityBenchShared;
using System.Runtime.CompilerServices;

namespace Burst.Compiler.IL.Tests
{
    /// <summary>
    /// Tests types
    /// </summary>
    internal partial class Types
    {
        [TestCompiler]
        public static int Bool()
        {
            return sizeof(bool);
        }

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolArgAndReturn(bool value)
        {
            return !value;
        }

        private static bool BoolArgAndReturnSubFunction(bool value)
        {
            return !value;
        }

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolArgAndReturnCall(bool value)
        {
            return BoolArgAndReturnSubFunction(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static bool BoolMarshalAsU1(bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsU1Call(bool value)
        {
            return BoolMarshalAsU1(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static bool BoolMarshalAsI1(bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsI1Call(bool value)
        {
            return BoolMarshalAsI1(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.R4)]
        private static bool BoolMarshalAsR4(bool b) => b;

        [TestCompiler(true, ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_MarshalAsNativeTypeOnReturnTypeNotSupported)]
        public static bool BoolMarshalAsR4Call(bool value)
        {
            return BoolMarshalAsR4(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool BoolMarshalAsU1Param([MarshalAs(UnmanagedType.U1)] bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsU1CallParam(bool value)
        {
            return BoolMarshalAsU1Param(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool BoolMarshalAsI1Param([MarshalAs(UnmanagedType.I1)] bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsI1CallParam(bool value)
        {
            return BoolMarshalAsI1Param(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool BoolMarshalAsR4Param([MarshalAs(UnmanagedType.R4)] bool b) => b;

        [TestCompiler(true, ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_MarshalAsOnParameterNotSupported)]
        public static bool BoolMarshalAsR4CallParam(bool value)
        {
            return BoolMarshalAsR4Param(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static bool BoolMarshalAsU1AndI1Param([MarshalAs(UnmanagedType.I1)] bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsU1AndI1CallParam(bool value)
        {
            return BoolMarshalAsU1AndI1Param(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static bool BoolMarshalAsI1AndU1Param([MarshalAs(UnmanagedType.U1)] bool b) => b;

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool BoolMarshalAsI1AndU1CallParam(bool value)
        {
            return BoolMarshalAsI1AndU1Param(value);
        }

        [TestCompiler]
        public static int Char()
        {
            return sizeof(char);
        }

        [TestCompiler]
        public static int Int8()
        {
            return sizeof(sbyte);
        }

        [TestCompiler]
        public static int Int16()
        {
            return sizeof(short);
        }

        [TestCompiler]
        public static int Int32()
        {
            return sizeof(int);
        }

        [TestCompiler]
        public static int Int64()
        {
            return sizeof(long);
        }

        [TestCompiler]
        public static int UInt8()
        {
            return sizeof(byte);
        }

        [TestCompiler]
        public static int UInt16()
        {
            return sizeof(ushort);
        }

        [TestCompiler]
        public static int UInt32()
        {
            return sizeof(uint);
        }

        [TestCompiler]
        public static int UInt64()
        {
            return sizeof(ulong);
        }

        [TestCompiler]
        public static int EnumSizeOf()
        {
            return sizeof(MyEnum);
        }


        [TestCompiler]
        public static int EnumByteSizeOf()
        {
            return sizeof(MyEnumByte);
        }

        [TestCompiler(MyEnumByte.Tada2)]
        public static MyEnumByte CheckEnumByte(ref MyEnumByte value)
        {
            // Check stloc for enum
            value = MyEnumByte.Tada1;
            return value;
        }

        [TestCompiler(MyEnum.Value15)]
        public static int EnumByParam(MyEnum value)
        {
            return 1 + (int)value;
        }

        [TestCompiler]
        public static float Struct()
        {
            var value = new MyStruct(1,2,3,4);
            return value.x + value.y + value.z + value.w;
        }

        [TestCompiler]
        public static long StructAccess()
        {
            var s = new InterleavedBoolStruct();
            s.b1 = true;
            s.i2 = -1;
            s.b3 = true;
            s.i5 = 3;
            return s.i5;
        }

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static bool StructWithBool(bool value)
        {
            // This method test that storage of boolean between local and struct is working
            // (as they could have different layout)
            var b = new BoolStruct();
            b.b1 = !value;
            return b.b1;
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_CallingManagedMethodNotSupported)]
        public static int TestUsingReferenceType()
        {
            return "this is not supported by burst".Length;
        }

        private struct MyStruct
        {
            public MyStruct(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public float x;
            public float y;
            public float z;
            public float w;
        }

        private struct BoolStruct
        {
#pragma warning disable 0649
            public bool b1;
            public bool b2;
#pragma warning restore 0649
        }

        private unsafe struct BoolFixedStruct
        {
#pragma warning disable 0649
            public fixed bool Values[16];
#pragma warning restore 0649
        }

        private struct InterleavedBoolStruct
        {
#pragma warning disable 0649
            public bool b1;
            public int i2;
            public bool b3;
            public bool b4;
            public long i5;
            public MyEnum e6;
#pragma warning restore 0649
        }

        public enum MyEnum
        {
            Value1 = 1,
            Value15 = 15,
        }


        [StructLayout(LayoutKind.Explicit)]
        private struct ExplicitLayoutStruct
        {
            [FieldOffset(0)]
            public int FieldA;

            [FieldOffset(0)]
            public int FieldB;
        }

        [StructLayout(LayoutKind.Sequential, Size = 1024)]
        private struct StructWithSize
        {
            public int FieldA;

            public int FieldB;
        }

        private struct EmptyStruct
        {
        }

        public enum MyEnumByte : byte
        {
            Tada1 = 1,

            Tada2 = 2
        }

        private static ValueTuple<int> ReturnValueTuple1() => ValueTuple.Create(42);

        [TestCompiler]
        public static long TestValueTuple1Return()
        {
            var tuple = ReturnValueTuple1();

            return tuple.Item1;
        }

        private static (int, uint) ReturnValueTuple2() => (42, 13);

        [TestCompiler]
        public static long TestValueTuple2Return()
        {
            var tuple = ReturnValueTuple2();

            return tuple.Item1 + tuple.Item2;
        }

        private static (int, uint, byte) ReturnValueTuple3() => (42, 13, 13);

        [TestCompiler]
        public static long TestValueTuple3Return()
        {
            var tuple = ReturnValueTuple3();

            return tuple.Item1 + tuple.Item2 + tuple.Item3;
        }

        private static (int, uint, byte, sbyte) ReturnValueTuple4() => (42, 13, 13, -13);

        [TestCompiler]
        public static long TestValueTuple4Return()
        {
            var tuple = ReturnValueTuple4();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4;
        }

        private static (int, uint, byte, sbyte, long) ReturnValueTuple5() => (42, 13, 13, -13, 53);

        [TestCompiler]
        public static long TestValueTuple5Return()
        {
            var tuple = ReturnValueTuple5();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5;
        }

        private struct SomeStruct
        {
            public int X;
        }

        private static (int, uint, byte, sbyte, long, SomeStruct) ReturnValueTuple6() => (42, 13, 13, -13, 535353, new SomeStruct { X = 42 } );

        [TestCompiler]
        public static long TestValueTuple6Return()
        {
            var tuple = ReturnValueTuple6();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X;
        }

        private static (int, uint, byte, sbyte, long, SomeStruct, short) ReturnValueTuple7() => (42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400);

        [TestCompiler]
        public static long TestValueTuple7Return()
        {
            var tuple = ReturnValueTuple7();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7;
        }

        private static (int, uint, byte, sbyte, long, SomeStruct, short, int) ReturnValueTuple8() => (42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400, -400);

        [TestCompiler]
        public static long TestValueTuple8Return()
        {
            var tuple = ReturnValueTuple8();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7 + tuple.Item8;
        }

        private static (int, uint, byte, sbyte, long, SomeStruct, short, int, long) ReturnValueTuple9() => (42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400, -400, 48);

        [TestCompiler]
        public static long TestValueTuple9Return()
        {
            var tuple = ReturnValueTuple9();

            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7 + tuple.Item8 + tuple.Item9;
        }

        private static long ValueTuple1Arg(ValueTuple<int> tuple)
        {
            return tuple.Item1;
        }

        [TestCompiler]
        public static long TestValueTuple1Arg()
        {
            return ValueTuple1Arg(ValueTuple.Create(42));
        }

        private static long ValueTuple2Arg((int, uint) tuple)
        {
            return tuple.Item1 + tuple.Item2;
        }

        [TestCompiler]
        public static long TestValueTuple2Arg()
        {
            return ValueTuple2Arg((42, 13));
        }

        private static long ValueTuple3Arg((int, uint, byte) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3;
        }

        [TestCompiler]
        public static long TestValueTuple3Arg()
        {
            return ValueTuple3Arg((42, 13, 13));
        }

        private static long ValueTuple4Arg((int, uint, byte, sbyte) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4;
        }

        [TestCompiler]
        public static long TestValueTuple4Arg()
        {
            return ValueTuple4Arg((42, 13, 13, -13));
        }

        private static long ValueTuple5Arg((int, uint, byte, sbyte, long) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5;
        }

        [TestCompiler]
        public static long TestValueTuple5Arg()
        {
            return ValueTuple5Arg((42, 13, 13, -13, 535353));
        }

        private static long ValueTuple6Arg((int, uint, byte, sbyte, long, SomeStruct) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X;
        }

        [TestCompiler]
        public static long TestValueTuple6Arg()
        {
            return ValueTuple6Arg((42, 13, 13, -13, 535353, new SomeStruct { X = 42 }));
        }

        private static long ValueTuple7Arg((int, uint, byte, sbyte, long, SomeStruct, short) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7;
        }

        [TestCompiler]
        public static long TestValueTuple7Arg()
        {
            return ValueTuple7Arg((42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400));
        }

        private static long ValueTuple8Arg((int, uint, byte, sbyte, long, SomeStruct, short, int) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7 + tuple.Item8;
        }

        [TestCompiler]
        public static long TestValueTuple8Arg()
        {
            return ValueTuple8Arg((42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400, -400));
        }

        private static long ValueTuple9Arg((int, uint, byte, sbyte, long, SomeStruct, short, int, long) tuple)
        {
            return tuple.Item1 + tuple.Item2 + tuple.Item3 + tuple.Item4 + tuple.Item5 + tuple.Item6.X + tuple.Item7 + tuple.Item8 + tuple.Item9;
        }

        [TestCompiler]
        public static long TestValueTuple9Arg()
        {
            return ValueTuple9Arg((42, 13, 13, -13, 535353, new SomeStruct { X = 42 }, 400, -400, 48));
        }

        // This needs to be here because the static delegate registry refers to it.
        public struct SomeStructWithValueTuple
        {
            public ValueTuple<int, float> X;

            public struct Provider : IArgumentProvider
            {
                public object Value => new SomeStructWithValueTuple { X = (42, 42.0f) };
            }
        }
    }
}
