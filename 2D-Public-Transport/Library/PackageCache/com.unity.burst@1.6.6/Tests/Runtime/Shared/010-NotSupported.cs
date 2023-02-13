using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    /// <summary>
    /// Tests types
    /// </summary>
    [BurstCompile]
    internal class NotSupported
    {
        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_OnlyStaticMethodsAllowed)]
        public int InstanceMethod()
        {
            return 1;
        }

        [TestCompiler(1, ExpectCompilerException = true, ExpectedDiagnosticIds = new[] { DiagnosticId.ERR_CallingManagedMethodNotSupported, DiagnosticId.ERR_FunctionPointerMethodAndTypeMissingBurstCompileAttribute })]
        public static int TestDelegate(int data)
        {
            return ProcessData(i => i + 1, data);
        }

        private static int ProcessData(Func<int, int> yo, int value)
        {
            return yo(value);
        }

        public struct HasMarshalAttribute
        {
            [MarshalAs(UnmanagedType.U1)] public bool A;
        }

        //[TestCompiler(ExpectCompilerException = true)]
        [TestCompiler] // Because MarshalAs is used in mathematics we cannot disable it for now
        public static void TestStructWithMarshalAs()
        {
#pragma warning disable 0219
            var x = new HasMarshalAttribute();
#pragma warning restore 0219
        }

        public struct HasMarshalAsSysIntAttribute
        {
            [MarshalAs(UnmanagedType.SysInt)] public bool A;
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_MarshalAsOnFieldNotSupported)]
        public static void TestStructWithMarshalAsSysInt()
        {
#pragma warning disable 0219
            var x = new HasMarshalAsSysIntAttribute();
#pragma warning restore 0219
        }

        [TestCompiler(42, ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_MarshalAsOnParameterNotSupported)]
        public static void TestMethodWithMarshalAsParameter([MarshalAs(UnmanagedType.I8)] int x)
        {
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_MarshalAsOnReturnTypeNotSupported)]
        [return: MarshalAs(UnmanagedType.I8)]
        public static int TestMethodWithMarshalAsReturnType()
        {
            return 42;
        }

        private static float3 a = new float3(1, 2, 3);

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_LoadingFromNonReadonlyStaticFieldNotSupported)]
        public static bool TestStaticLoad()
        {
            var cmp = a == new float3(1, 2, 3);

            return cmp.x && cmp.y && cmp.z;
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_LoadingFromManagedNonReadonlyStaticFieldNotSupported)]
        public static void TestStaticStore()
        {
            a.x = 42;
        }

        private interface ISomething
        {
            void DoSomething();
        }

        private struct Something : ISomething
        {
            public byte A;

            public void DoSomething()
            {
                A = 42;
            }
        }

        private static ISomething something = new Something { A = 13 };

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_LoadingFromManagedNonReadonlyStaticFieldNotSupported)]
        public static void TestStaticInterfaceStore()
        {
            something.DoSomething();
        }

        private static int i = 42;

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_LoadingFromNonReadonlyStaticFieldNotSupported)]
        public static int TestStaticIntLoad()
        {
            return i;
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_InstructionStsfldNotSupported)]
        public static void TestStaticIntStore()
        {
            i = 13;
        }

        public delegate char CharbyValueDelegate(char c);

#if BURST_TESTS_ONLY
        [BurstCompile]
#endif
        public static char CharbyValue(char c)
        {
            return c;
        }

        public struct CharbyValueFunc : IFunctionPointerProvider
        {
            public FunctionPointer<CharbyValueDelegate> FunctionPointer;

            public object FromIntPtr(IntPtr ptr)
            {
                return new CharbyValueFunc() { FunctionPointer = new FunctionPointer<CharbyValueDelegate>(ptr) };
            }
        }

        [TestCompiler(nameof(CharbyValue), 0x1234, ExpectCompilerException = true, ExpectedDiagnosticIds = new[] { DiagnosticId.ERR_TypeNotBlittableForFunctionPointer, DiagnosticId.ERR_StructsWithNonUnicodeCharsNotSupported })]
        public static int TestCharbyValue(ref CharbyValueFunc fp, int i)
        {
            var c = (char)i;
            return fp.FunctionPointer.Invoke(c);
        }

        struct Halfs
        {
            public static readonly half3 h3_h = new half3(new half(42.0f));
            public static readonly half3 h3_d = new half3(0.5);
            public static readonly half3 h3_v2s = new half3(new half2(new half(1.0f), new half(2.0f)), new half(0.5f));
            public static readonly half3 h3_sv2 = new half3(new half(0.5f), new half2(new half(1.0f), new half(2.0f)));
            public static readonly half3 h3_v3 = new half3(new half(0.5f), new half(42.0f), new half(13.0f));
        }

        [TestCompiler]
        public static float TestStaticHalf3()
        {
            var result = (float3)Halfs.h3_h + Halfs.h3_d + Halfs.h3_v2s + Halfs.h3_sv2 + Halfs.h3_v3;
            return result.x + result.y + result.z;
        }

        [TestCompiler(42, 13, ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_AssertTypeNotSupported)]
        public static void TestAreEqual(int a, int b)
        {
            Assert.AreEqual(a, b, "unsupported", new object[0]);
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_InstructionLdtokenTypeNotSupported)]
        public static bool TestTypeof()
        {
            return typeof(int).IsPrimitive;
        }

        public class AwfulClass
        {
            public int Foo;
        }

        public struct BetterStruct
        {
            public int Foo;
        }

        public struct MixedStaticInits
        {
            public static readonly AwfulClass AC = new AwfulClass { Foo = 42 };
            public static readonly BetterStruct BS = new BetterStruct { Foo = 42 };
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticIds = new[] { DiagnosticId.ERR_InstructionNewobjWithManagedTypeNotSupported, DiagnosticId.ERR_ManagedStaticConstructor })]
        public static int TestMixedStaticInits()
        {
            return MixedStaticInits.BS.Foo;
        }

        public struct StaticArrayWrapper
        {
            private const int ArrayLength = 4;
            public static readonly int[] StaticArray = new int[4];

            static StaticArrayWrapper()
            {
                for (int i = 0; i < ArrayLength; ++i)
                {
                    StaticArray[i] = i;
                }
            }
        }

        [TestCompiler]
        public unsafe static int TestStaticArrayWrapper()
        {
            return StaticArrayWrapper.StaticArray[0];
        }

        class NestedArrayHolder
        {
            public static readonly int4[][] SomeOffsetThing =
            {
                new[] {new int4(0), new int4(0, 0, 1, 0), new int4(0, 1, 0, 0), new int4(0, 1, 1, 0)},
                new[] {new int4(0), new int4(1, 0, 0, 0), new int4(0, 0, 1, 0), new int4(1, 0, 1, 0)},
                new[] {new int4(0), new int4(0, 1, 0, 0), new int4(1, 0, 0, 0), new int4(1, 1, 0, 0)},
            };
        }

        [TestCompiler]
        public unsafe static int TestNestedManagedArrays()
        {
            return NestedArrayHolder.SomeOffsetThing[0][0].x;
        }

        public static readonly int[,] SomeMultiDimensionalThing = new int[2, 4]
        {
            { 1, 2, 3, 4 },
            { -1, -2, -3, -4 },
        };

        [TestCompiler(ExpectCompilerException =  true, ExpectedDiagnosticIds = new[] { DiagnosticId.ERR_ConstructorNotSupported, DiagnosticId.ERR_MultiDimensionalArrayUnsupported })]
        public static int TestMultiDimensionalArray() => SomeMultiDimensionalThing[1, 1];
    }
}
