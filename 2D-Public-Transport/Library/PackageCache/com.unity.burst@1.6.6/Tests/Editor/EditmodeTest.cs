using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.TestTools;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Threading;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using Unity.Profiling;

[TestFixture]
public class EditModeTest
{
    private const int MaxIterations = 500;

    #if UNITY_2019_3_OR_NEWER
    [UnityTest]
    public IEnumerator CheckBurstJobEnabledDisabled()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        try
        {
            foreach(var item in CheckBurstJobDisabled()) yield return item;
            foreach(var item in CheckBurstJobEnabled()) yield return item;
        }
        finally
        {
            BurstCompiler.Options.EnableBurstCompilation = true;
        }
    }
#endif

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

#if UNITY_2019_3_OR_NEWER
    [UnityTest]
    public IEnumerator CheckJobWithNativeArray()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        var job = new BurstJobTester2.MyJobCreatingAndDisposingNativeArray()
        {
            Length = 128,
            Result = new NativeArray<int>(16, Allocator.TempJob)
        };
        var handle = job.Schedule();
        handle.Complete();
        try
        {
            Assert.AreEqual(job.Length, job.Result[0]);
        }
        finally
        {
            job.Result.Dispose();
        }
    }
#endif


#if UNITY_BURST_BUG_FUNCTION_POINTER_FIXED
    [UnityTest]
    public IEnumerator CheckBurstFunctionPointerException()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        using (var jobTester = new BurstJobTester())
        {
            var exception = Assert.Throws<InvalidOperationException>(() => jobTester.CheckFunctionPointer());
            StringAssert.Contains("Exception was thrown from a function compiled with Burst", exception.Message);
        }
    }
#endif

    [BurstCompile(CompileSynchronously = true)]
    private struct HashTestJob : IJob
    {
        public NativeArray<int> Hashes;

        public void Execute()
        {
            Hashes[0] = BurstRuntime.GetHashCode32<int>();
            Hashes[1] = TypeHashWrapper.GetIntHash();

            Hashes[2] = BurstRuntime.GetHashCode32<TypeHashWrapper.SomeStruct<int>>();
            Hashes[3] = TypeHashWrapper.GetGenericHash<int>();
        }
    }

    [Test]
    public static void TestTypeHash()
    {
        HashTestJob job = new HashTestJob
        {
            Hashes = new NativeArray<int>(4, Allocator.TempJob)
        };
        job.Schedule().Complete();

        var hash0 = job.Hashes[0];
        var hash1 = job.Hashes[1];

        var hash2 = job.Hashes[2];
        var hash3 = job.Hashes[3];

        job.Hashes.Dispose();

        Assert.AreEqual(hash0, hash1, "BurstRuntime.GetHashCode32<int>() has returned two different hashes");
        Assert.AreEqual(hash2, hash3, "BurstRuntime.GetHashCode32<SomeStruct<int>>() has returned two different hashes");
    }

#if UNITY_2019_3_OR_NEWER
    [UnityTest]
    public IEnumerator CheckSafetyChecksWithDomainReload()
    {
        {
            var job = new SafetyCheckJobWithDomainReload();
            {
                // Run with safety-checks true
                BurstCompiler.Options.EnableBurstSafetyChecks = true;
                job.Result = new NativeArray<int>(1, Allocator.TempJob);
                try
                {
                    var handle = job.Schedule();
                    handle.Complete();
                    Assert.AreEqual(2, job.Result[0]);
                }
                finally
                {
                    job.Result.Dispose();
                }
            }

            {
                // Run with safety-checks false
                BurstCompiler.Options.EnableBurstSafetyChecks = false;
                job.Result = new NativeArray<int>(1, Allocator.TempJob);
                bool hasException = false;
                try
                {
                    var handle = job.Schedule();
                    handle.Complete();
                    Assert.AreEqual(1, job.Result[0]);
                }
                catch
                {
                    hasException = true;
                    throw;
                }
                finally
                {
                    job.Result.Dispose();
                    if (hasException)
                    {
                        BurstCompiler.Options.EnableBurstSafetyChecks = true;
                    }
                }
            }
        }

        // Ask for domain reload
        EditorUtility.RequestScriptReload();

        // Wait for the domain reload to be completed
        yield return new WaitForDomainReload();

        {
            // The safety checks should have been disabled by the previous code
            Assert.False(BurstCompiler.Options.EnableBurstSafetyChecks);
            // Restore safety checks
            BurstCompiler.Options.EnableBurstSafetyChecks = true;
        }
    }
#endif

    [BurstCompile(CompileSynchronously = true)]
    private struct DebugLogJob : IJob
    {
        public int Value;

        public void Execute()
        {
            UnityEngine.Debug.Log($"This is a string logged from a job with burst with the following {Value}");
        }
    }

    [Test]
    public static void TestDebugLog()
    {
        var job = new DebugLogJob
        {
            Value = 256
        };
        job.Schedule().Complete();
    }


    [BurstCompile(CompileSynchronously = true)]
    private struct SafetyCheckJobWithDomainReload : IJob
    {
        public NativeArray<int> Result;

        public void Execute()
        {
            Result[0] = 1;
            SetResultWithSafetyChecksOnly();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void SetResultWithSafetyChecksOnly()
        {
            Result[0] = 2;
        }
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void SafelySetSomeBool(ref bool b)
    {
        b = true;
    }

    [BurstCompile(DisableSafetyChecks = false)]
    private struct EnabledSafetyChecksJob : IJob
    {
        [WriteOnly] public NativeArray<int> WasHit;

        public void Execute()
        {
            var b = false;
            SafelySetSomeBool(ref b);
            WasHit[0] = b ? 1 : 0;
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    private struct DisabledSafetyChecksJob : IJob
    {
        [WriteOnly] public NativeArray<int> WasHit;

        public void Execute()
        {
            var b = false;
            SafelySetSomeBool(ref b);
            WasHit[0] = b ? 1 : 0;
        }
    }

#if UNITY_2019_3_OR_NEWER
    [UnityTest]
    public IEnumerator CheckSafetyChecksOffGloballyAndOnInJob()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = false;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var job = new EnabledSafetyChecksJob()
        {
            WasHit = new NativeArray<int>(1, Allocator.TempJob)
        };

        job.Schedule().Complete();

        try
        {
            // Safety checks are off globally which overwrites the job having safety checks on.
            Assert.AreEqual(0, job.WasHit[0]);
        }
        finally
        {
            job.WasHit.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOffGloballyAndOffInJob()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = false;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var job = new DisabledSafetyChecksJob()
        {
            WasHit = new NativeArray<int>(1, Allocator.TempJob)
        };

        job.Schedule().Complete();

        try
        {
            // Safety checks are off globally and off in job.
            Assert.AreEqual(0, job.WasHit[0]);
        }
        finally
        {
            job.WasHit.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOnGloballyAndOnInJob()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = true;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var job = new EnabledSafetyChecksJob()
        {
            WasHit = new NativeArray<int>(1, Allocator.TempJob)
        };

        job.Schedule().Complete();

        try
        {
            // Safety checks are on globally and on in job.
            Assert.AreEqual(1, job.WasHit[0]);
        }
        finally
        {
            job.WasHit.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOnGloballyAndOffInJob()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = true;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var job = new DisabledSafetyChecksJob()
        {
            WasHit = new NativeArray<int>(1, Allocator.TempJob)
        };

        job.Schedule().Complete();

        try
        {
            // Safety checks are on globally but off in job.
            Assert.AreEqual(0, job.WasHit[0]);
        }
        finally
        {
            job.WasHit.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator CheckForceSafetyChecksWorks()
    {
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = true;

        yield return null;

        var job = new DisabledSafetyChecksJob()
        {
            WasHit = new NativeArray<int>(1, Allocator.TempJob)
        };

        job.Schedule().Complete();

        try
        {
            // Even though the job has set disabled safety checks, the menu item 'Force On'
            // has been set which overrides any other requested behaviour.
            Assert.AreEqual(1, job.WasHit[0]);
        }
        finally
        {
            job.WasHit.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator CheckSharedStaticWithDomainReload()
    {
        // Check that on a first access, SharedStatic is always empty
        AssertTestSharedStaticEmpty();

        // Fill with some data
        TestSharedStatic.SharedValue.Data = new TestSharedStatic(1, 2, 3, 4);

        Assert.AreEqual(1, TestSharedStatic.SharedValue.Data.Value1);
        Assert.AreEqual(2, TestSharedStatic.SharedValue.Data.Value2);
        Assert.AreEqual(3, TestSharedStatic.SharedValue.Data.Value3);
        Assert.AreEqual(4, TestSharedStatic.SharedValue.Data.Value4);

        // Ask for domain reload
        EditorUtility.RequestScriptReload();

        // Wait for the domain reload to be completed
        yield return new WaitForDomainReload();

        // Make sure that after a domain reload everything is initialized back to zero
        AssertTestSharedStaticEmpty();
    }

    private static void AssertTestSharedStaticEmpty()
    {
        Assert.AreEqual(0, TestSharedStatic.SharedValue.Data.Value1);
        Assert.AreEqual(0, TestSharedStatic.SharedValue.Data.Value2);
        Assert.AreEqual(0, TestSharedStatic.SharedValue.Data.Value3);
        Assert.AreEqual(0, TestSharedStatic.SharedValue.Data.Value4);
    }

    private struct TestSharedStatic
    {
        public static readonly SharedStatic<TestSharedStatic> SharedValue = SharedStatic<TestSharedStatic>.GetOrCreate<TestSharedStatic>();

        public TestSharedStatic(int value1, long value2, long value3, long value4)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
            Value4 = value4;
        }

        public int Value1;
        public long Value2;
        public long Value3;
        public long Value4;
    }

    static EditModeTest()
    {
        // UnityEngine.Debug.Log("Domain Reload");
    }
    [BurstCompile]
    private static class FunctionPointers
    {
        public delegate int SafetyChecksDelegate();

        [BurstCompile(DisableSafetyChecks = false)]
        public static int WithSafetyChecksEnabled()
        {
            var b = false;
            SafelySetSomeBool(ref b);
            return b ? 1 : 0;
        }

        [BurstCompile(DisableSafetyChecks = true)]
        public static int WithSafetyChecksDisabled()
        {
            var b = false;
            SafelySetSomeBool(ref b);
            return b ? 1 : 0;
        }
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOffGloballyAndOffInFunctionPointer()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = false;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var funcPtr = BurstCompiler.CompileFunctionPointer<FunctionPointers.SafetyChecksDelegate>(FunctionPointers.WithSafetyChecksDisabled);

        // Safety Checks are off globally and off in the job.
        Assert.AreEqual(0, funcPtr.Invoke());
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOffGloballyAndOnInFunctionPointer()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = false;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var funcPtr = BurstCompiler.CompileFunctionPointer<FunctionPointers.SafetyChecksDelegate>(FunctionPointers.WithSafetyChecksEnabled);

        // Safety Checks are off globally and on in job, but the global setting takes precedence.
        Assert.AreEqual(0, funcPtr.Invoke());
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOnGloballyAndOffInFunctionPointer()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = true;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var funcPtr = BurstCompiler.CompileFunctionPointer<FunctionPointers.SafetyChecksDelegate>(FunctionPointers.WithSafetyChecksDisabled);

        // Safety Checks are on globally and off in the job, so the job takes predence.
        Assert.AreEqual(0, funcPtr.Invoke());
    }

    [UnityTest]
    public IEnumerator CheckSafetyChecksOnGloballyAndOnInFunctionPointer()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = true;
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;

        yield return null;

        var funcPtr = BurstCompiler.CompileFunctionPointer<FunctionPointers.SafetyChecksDelegate>(FunctionPointers.WithSafetyChecksEnabled);

        // Safety Checks are on globally and on in the job.
        Assert.AreEqual(1, funcPtr.Invoke());
    }

    [UnityTest]
    public IEnumerator CheckFunctionPointerForceSafetyChecksWorks()
    {
        BurstCompiler.Options.ForceEnableBurstSafetyChecks = true;

        yield return null;

        var funcPtr = BurstCompiler.CompileFunctionPointer<FunctionPointers.SafetyChecksDelegate>(FunctionPointers.WithSafetyChecksDisabled);

        // Even though the job has set disabled safety checks, the menu item 'Force On'
        // has been set which overrides any other requested behaviour.
        Assert.AreEqual(1, funcPtr.Invoke());
    }
#endif

#if UNITY_2020_1_OR_NEWER
    [BurstCompile(CompileSynchronously = true)]
    private struct DebugDrawLineJob : IJob
    {
        public void Execute()
        {
            Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(5, 0, 0), Color.green);
        }
    }

    [Test]
    public void TestDebugDrawLine()
    {
        var job = new DebugDrawLineJob();
        job.Schedule().Complete();
    }
#endif

#if UNITY_2020_2_OR_NEWER
    [BurstCompile]
    private static class ProfilerMarkerWrapper
    {
        private static readonly ProfilerMarker StaticMarker = new ProfilerMarker("TestStaticBurst");

        [BurstCompile(CompileSynchronously = true)]
        public static int CreateAndUseProfilerMarker(int start)
        {
            using (StaticMarker.Auto())
            {
                var p = new ProfilerMarker("TestBurst");
                p.Begin();
                var result = 0;
                for (var i = start; i < start + 100000; i++)
                {
                    result += i;
                }
                p.End();
                return result;
            }
        }
    }

    private delegate int IntReturnIntDelegate(int param);

    [Test]
    public void TestCreateProfilerMarker()
    {
        var fp = BurstCompiler.CompileFunctionPointer<IntReturnIntDelegate>(ProfilerMarkerWrapper.CreateAndUseProfilerMarker);
        fp.Invoke(5);
    }
#endif
}