using System.Collections;
using NUnit.Framework;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.TestTools;
using System;
using Unity.Jobs;

[TestFixture]
public class PlaymodeTest
{
//    [UnityTest]
    public IEnumerator CheckBurstJobEnabledDisabled()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
#if UNITY_2019_3_OR_NEWER
        foreach(var item in CheckBurstJobDisabled()) yield return item;
#endif
        foreach(var item in CheckBurstJobEnabled()) yield return item;
    }

    private IEnumerable CheckBurstJobEnabled()
    {
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        using (var jobTester = new BurstJobTester2())
        {
            var result = jobTester.Calculate();
            Assert.AreNotEqual(0.0f, result);
        }
    }

    private IEnumerable CheckBurstJobDisabled()
    {
        BurstCompiler.Options.EnableBurstCompilation = false;

        yield return null;

        using (var jobTester = new BurstJobTester2())
        {
            var result = jobTester.Calculate();
            Assert.AreEqual(0.0f, result);
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    private struct ThrowingJob : IJob
    {
        public int I;

        public void Execute()
        {
            if (I < 0)
            {
                throw new System.Exception("Some Exception!");
            }
        }
    }

    [Test]
    public void NoSafetyCheckExceptionWarningInEditor()
    {
        var job = new ThrowingJob { I = 42 };
        job.Schedule().Complete();

        // UNITY_BURST_DEBUG enables additional logging which messes with our check.
        if (null == System.Environment.GetEnvironmentVariable("UNITY_BURST_DEBUG"))
        {
            LogAssert.NoUnexpectedReceived();
        }
    }

#if UNITY_2019_3_OR_NEWER
    [BurstCompile]
    public struct SomeFunctionPointers
    {
        [BurstDiscard]
        private static void MessWith(ref int a) => a += 13;

        [BurstCompile]
        public static int A(int a, int b)
        {
            MessWith(ref a);
            return a + b;
        }

        [BurstCompile(DisableDirectCall = true)]
        public static int B(int a, int b)
        {
            MessWith(ref a);
            return a - b;
        }

        [BurstCompile(CompileSynchronously = true)]
        public static int C(int a, int b)
        {
            MessWith(ref a);
            return a * b;
        }

        [BurstCompile(CompileSynchronously = true, DisableDirectCall = true)]
        public static int D(int a, int b)
        {
            MessWith(ref a);
            return a / b;
        }

        public delegate int Delegate(int a, int b);
    }

    [Test]
    public void TestDirectCalls()
    {
        Assert.IsTrue(BurstCompiler.IsEnabled);

        // a can either be (42 + 13) + 53 or 42 + 53 (depending on whether it was burst compiled).
        var a = SomeFunctionPointers.A(42, 53);
        Assert.IsTrue((a == ((42 + 13) + 53)) || (a == (42 + 53)));

        // b can only be (42 + 13) - 53, because direct call is disabled and so we always call the managed method.
        var b = SomeFunctionPointers.B(42, 53);
        Assert.AreEqual((42 + 13) - 53, b);

        // c can only be 42 * 53, because synchronous compilation is enabled.
        var c = SomeFunctionPointers.C(42, 53);
        Assert.AreEqual(42 * 53, c);

        // d can only be (42 + 13) / 53, because even though synchronous compilation is enabled, direct call is disabled.
        var d = SomeFunctionPointers.D(42, 53);
        Assert.AreEqual((42 + 13) / 53, d);
    }

    [Test]
    public void TestDirectCallInNamespacedClass()
    {
        void onCompileILPPMethod()
        {
            Assert.Fail("BurstCompiler.CompileILPPMethod should not have been called at this time");
        }

        // We expect BurstCompiler.CompileILPPMethod2 to have been called at startup, via
        // [InitializeOnLoad] or [RuntimeInitializeOnLoadMethod]. If it's called when we invoke
        // N.C.A(), then something has gone wrong.

        try
        {
            BurstCompiler.OnCompileILPPMethod += onCompileILPPMethod;

            var result = N.C.A();
            Assert.AreEqual(42, result);
        }
        finally
        {
            BurstCompiler.OnCompileILPPMethod -= onCompileILPPMethod;
        }
    }

    [Test]
    public void TestFunctionPointers()
    {
        Assert.IsTrue(BurstCompiler.IsEnabled);

        var A = BurstCompiler.CompileFunctionPointer<SomeFunctionPointers.Delegate>(SomeFunctionPointers.A);
        var B = BurstCompiler.CompileFunctionPointer<SomeFunctionPointers.Delegate>(SomeFunctionPointers.B);
        var C = BurstCompiler.CompileFunctionPointer<SomeFunctionPointers.Delegate>(SomeFunctionPointers.C);
        var D = BurstCompiler.CompileFunctionPointer<SomeFunctionPointers.Delegate>(SomeFunctionPointers.D);

        // a can either be (42 + 13) + 53 or 42 + 53 (depending on whether it was burst compiled).
        var a = A.Invoke(42, 53);
        Assert.IsTrue((a == ((42 + 13) + 53)) || (a == (42 + 53)));

        // b can either be (42 + 13) - 53 or 42 - 53 (depending on whether it was burst compiled).
        var b = B.Invoke(42, 53);
        Assert.IsTrue((b == ((42 + 13) - 53)) || (b == (42 - 53)));

        // c can only be 42 * 53, because synchronous compilation is enabled.
        var c = C.Invoke(42, 53);
        Assert.AreEqual(42 * 53, c);

        // d can only be 42 / 53, because synchronous compilation is enabled.
        var d = D.Invoke(42, 53);
        Assert.AreEqual(42 / 53, d);
    }

    [BurstCompile]
    public static class GenericClass<T>
    {
        [BurstCompile]
        public static int ConcreteMethod() => 3;
    }

    public delegate int NoArgsIntReturnDelegate();

    [Test]
    public void TestGenericClassConcreteMethodFunctionPointer()
    {
        Assert.IsTrue(BurstCompiler.IsEnabled);
        var F = BurstCompiler.CompileFunctionPointer<NoArgsIntReturnDelegate>(GenericClass<int>.ConcreteMethod);
        Assert.AreEqual(3, F.Invoke());
    }
#endif
}

#if UNITY_2019_3_OR_NEWER
// This test class is intentionally in a namespace to ensure that our
// direct-call [RuntimeInitializeOnLoadMethod] works correctly in that
// scenario.
namespace N
{
    [BurstCompile]
    internal static class C
    {
        public static int A() => B();

        [BurstCompile(CompileSynchronously = true)]
        private static int B()
        {
            var x = 42;
            DiscardedMethod(ref x);
            return x;
        }

        [BurstDiscard]
        private static void DiscardedMethod(ref int x)
        {
            x += 1;
        }
    }
}
#endif