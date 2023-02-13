// Doesn't work with IL2CPP yet - waiting for Unity fix to land.
#if BURST_INTERNAL //|| UNITY_2021_2_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using UnityBenchShared;

namespace Burst.Compiler.IL.Tests
{
    [BurstCompile]
    internal class TestCSharpFunctionPointers
    {
        [TestCompiler]
        public static unsafe int TestCSharpFunctionPointer()
        {
            delegate* unmanaged[Cdecl]<int, int> callback = &TestCSharpFunctionPointerCallback;
            return TestCSharpFunctionPointerHelper(callback);
        }

        private static unsafe int TestCSharpFunctionPointerHelper(delegate* unmanaged[Cdecl]<int, int> callback)
        {
            return callback(5);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        [BurstCompile]
        private static int TestCSharpFunctionPointerCallback(int value) => value + 1;

        [TestCompiler]
        public static unsafe int TestCSharpFunctionPointerCastingParameterPtrFromVoid()
        {
            delegate* unmanaged[Cdecl]<void*, int> callback = &TestCSharpFunctionPointerCallbackVoidPtr;
            delegate* unmanaged[Cdecl]<int*, int> callbackCasted = callback;

            int i = 5;

            return callbackCasted(&i);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        [BurstCompile]
        private static unsafe int TestCSharpFunctionPointerCallbackVoidPtr(void* value) => *((int*)value) + 1;

        [TestCompiler]
        public static unsafe int TestCSharpFunctionPointerCastingParameterPtrToVoid()
        {
            delegate* unmanaged[Cdecl]<int*, int> callback = &TestCSharpFunctionPointerCallbackIntPtr;
            delegate* unmanaged[Cdecl]<void*, int> callbackCasted = (delegate* unmanaged[Cdecl]<void*, int>)callback;

            int i = 5;

            return callbackCasted(&i);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        [BurstCompile]
        private static unsafe int TestCSharpFunctionPointerCallbackIntPtr(int* value) => *value + 1;

        [TestCompiler]
        public static unsafe int TestCSharpFunctionPointerCastingToAndFromVoidPtr()
        {
            delegate* unmanaged[Cdecl]<int*, int> callback = &TestCSharpFunctionPointerCallbackIntPtr;
            void* callbackAsVoidPtr = callback;
            delegate* unmanaged[Cdecl]<int*, int> callbackCasted = (delegate* unmanaged[Cdecl]<int*, int>)callbackAsVoidPtr;

            int i = 5;

            return callbackCasted(&i);
        }

        public struct CSharpFunctionPointerProvider : IArgumentProvider
        {
            public unsafe object Value
            {
                get
                {
                    delegate* unmanaged[Cdecl]<int, int> callback = &TestCSharpFunctionPointerCallback;
                    return (IntPtr)callback;
                }
            }
        }

        [TestCompiler(typeof(CSharpFunctionPointerProvider))]
        public static unsafe int TestCSharpFunctionPointerPassedInFromOutside(IntPtr callbackAsIntPtr)
        {
            delegate* unmanaged[Cdecl]<int, int> callback = (delegate* unmanaged[Cdecl]<int, int>)callbackAsIntPtr;
            return TestCSharpFunctionPointerHelper(callback);
        }

        private struct TestCSharpFunctionPointerWithStructParameterStruct
        {
            public int X;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        [BurstCompile]
        private static int TestCSharpFunctionPointerWithStructParameterCallback(TestCSharpFunctionPointerWithStructParameterStruct value) => value.X + 1;

        public struct CSharpFunctionPointerWithStructParameterProvider : IArgumentProvider
        {
            public unsafe object Value
            {
                get
                {
                    delegate* unmanaged[Cdecl]<TestCSharpFunctionPointerWithStructParameterStruct, int> callback = &TestCSharpFunctionPointerWithStructParameterCallback;
                    return (IntPtr)callback;
                }
            }
        }

        [TestCompiler(typeof(CSharpFunctionPointerWithStructParameterProvider))]
        public static unsafe int TestCSharpFunctionPointerPassedInFromOutsideWithStructParameter(IntPtr untypedFp)
        {
            return TestHashingFunctionPointerTypeHelper((delegate* unmanaged[Cdecl]<TestCSharpFunctionPointerWithStructParameterStruct, int>)untypedFp);
        }

        private static unsafe int TestHashingFunctionPointerTypeHelper(delegate* unmanaged[Cdecl]<TestCSharpFunctionPointerWithStructParameterStruct, int> fp)
        {
            return fp(new TestCSharpFunctionPointerWithStructParameterStruct { X = 42 });
        }

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_CalliNonCCallingConventionNotSupported)]
        public static unsafe int TestCSharpFunctionPointerInvalidCallingConvention()
        {
            delegate*<int, int> callback = &TestCSharpFunctionPointerInvalidCallingConventionCallback;
            return callback(5);
        }

        [BurstCompile]
        private static int TestCSharpFunctionPointerInvalidCallingConventionCallback(int value) => value + 1;

        [TestCompiler(ExpectCompilerException = true, ExpectedDiagnosticId = DiagnosticId.ERR_FunctionPointerMethodMissingBurstCompileAttribute)]
        public static unsafe int TestCSharpFunctionPointerMissingBurstCompileAttribute()
        {
            delegate* unmanaged[Cdecl]<int, int> callback = &TestCSharpFunctionPointerCallbackMissingBurstCompileAttribute;
            return callback(5);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int TestCSharpFunctionPointerCallbackMissingBurstCompileAttribute(int value) => value + 1;

#if BURST_TESTS_ONLY
        [DllImport("burst-dllimport-native")]
        private static extern unsafe int callFunctionPointer(delegate* unmanaged[Cdecl]<int, int> f);

        // Ignored on wasm since dynamic linking is not supported at present.
        // Override result on Mono because it throws a StackOverflowException for some reason related to the function pointer.
        // We should use OverrideResultOnMono, but OverrideResultOnMono still runs the managed version, which causes a crash,
        // so we use OverrideManagedResult.
        [TestCompiler(IgnoreOnPlatform = Backend.TargetPlatform.Wasm, OverrideManagedResult = 43)]
        public static unsafe int TestPassingFunctionPointerToNativeCode()
        {
            return callFunctionPointer(&TestCSharpFunctionPointerCallback);
        }
#endif
    }
}

// This attribute is also included in com.unity.burst/Tests/Runtime/FunctionPointerTests.cs,
// so we want to exclude it here when we're running inside Unity otherwise we'll get a
// duplicate definition error.
#if BURST_TESTS_ONLY
// UnmanagedCallersOnlyAttribute is new in .NET 5.0. This attribute is required
// when you declare an unmanaged function pointer with an explicit calling convention.
// Fortunately, Roslyn lets us declare the attribute class ourselves, and it will be used.
// Users will need this same declaration in their own projects, in order to use
// C# 9.0 function pointers.
namespace System.Runtime.InteropServices
{
    [AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public Type[] CallConvs;
    }
}
#endif
#endif