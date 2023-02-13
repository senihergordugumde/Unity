using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Editor;
using Unity.Jobs;

// This concrete generic type is only referenced in this assembly-level attribute,
// not anywhere else in code. This is to test that such types can be picked up
// by BurstReflection.
[assembly: BurstReflectionTests.RegisterGenericJobType(typeof(BurstReflectionTests.GenericParallelForJob<int>))]

[TestFixture]
public class BurstReflectionTests
{
    private List<System.Reflection.Assembly> _assemblies;

    [OneTimeSetUp]
    public void SetUp()
    {
        _assemblies = BurstReflection.EditorAssembliesThatCanPossiblyContainJobs;
    }

    [Test]
    public void CanGetAssemblyList()
    {
        Assert.That(_assemblies, Has.Count.GreaterThan(0));
    }

    [Test]
    [TestCase("BurstReflectionTests.MyJob - (IJob)")]
    [TestCase("BurstReflectionTests.MyGenericJob`1[System.Int32] - (IJob)")]
    [TestCase("BurstReflectionTests.MyGenericJob2`1[System.Int32] - (BurstReflectionTests.IMyGenericJob`1[System.Int32])")]
    [TestCase("BurstReflectionTests.MyGenericJob2`1[System.Double] - (BurstReflectionTests.IMyGenericJob`1[System.Double])")]
    [TestCase("BurstReflectionTests.NonGenericType.TestMethod1()")]
    [TestCase("BurstReflectionTests.GenericType`1[System.Int32].TestMethod1()")]
    [TestCase("BurstReflectionTests.GenericType`1.NestedNonGeneric[System.Int32].TestMethod2()")]
    [TestCase("BurstReflectionTests.GenericType`1.NestedGeneric`1[System.Int32,System.Single].TestMethod3()")]
    [TestCase("BurstReflectionTests.MyGenericJobSeparateAssembly`1[System.Int32] - (BurstReflectionTestsSeparateAssembly.IMyGenericJobSeparateAssembly`1[System.Int32])")]
    [TestCase("BurstReflectionTests.GenericParallelForJob`1[System.Int32] - (IJobParallelFor)")]
    public void CanFindJobType(string compileTargetName)
    {
        var result = BurstReflection.FindExecuteMethods(_assemblies, BurstReflectionAssemblyOptions.None);

        Assert.That(result.LogMessages, Is.Empty);

        var compileTarget = result.CompileTargets.Find(x => x.GetDisplayName() == compileTargetName);
        Assert.That(compileTarget, Is.Not.Null);
    }

    [BurstCompile]
    private struct MyJob : IJob
    {
        public void Execute() { }
    }

    [BurstCompile]
    private struct MyGenericJob<T> : IJob
    {
        public void Execute() { }

        private static void UseConcreteType()
        {
            new MyGenericJob<int>().Execute();
        }
    }

    [Unity.Jobs.LowLevel.Unsafe.JobProducerType(typeof(MyJobProducer<,>))]
    private interface IMyGenericJob<T>
    {
        void Execute();
    }

    [BurstCompile]
    private struct MyGenericJob2<T> : IMyGenericJob<T>
    {
        public void Execute() { }

        private static void UseConcreteType()
        {
            new MyGenericJob2<int>().Execute();
        }
    }

    private static class MyJobProducer<TJob, T>
    {
        public static void Execute(ref TJob job)
        {

        }
    }

    private struct MyGenericJob2Wrapper<T1, T2>
    {
        public MyGenericJob2<T2> Job;

        private static void UseConcreteType()
        {
            var x = new MyGenericJob2Wrapper<float, double>();
            x.Job.Execute();
        }
    }

    [BurstCompile]
    private struct NonGenericType
    {
        [BurstCompile]
        public static void TestMethod1() { }
    }

    [BurstCompile]
    private struct GenericType<T>
    {
        public static Action Delegate1;
        public static Action Delegate2;
        public static Action Delegate3;

        [BurstCompile]
        public static void TestMethod1() { }

        [BurstCompile]
        public class NestedNonGeneric
        {
            [BurstCompile]
            public static void TestMethod2() { }
        }

        [BurstCompile]
        public class NestedGeneric<T2>
        {
            [BurstCompile]
            public static void TestMethod3() { }
        }

        private static void UseConcreteType()
        {
            // Store the delegates to static fields to avoid
            // them being optimized-away in Release builds
            Delegate1 = GenericType<int>.TestMethod1;
            Delegate2 = GenericType<int>.NestedNonGeneric.TestMethod2;
            Delegate3 = GenericType<int>.NestedGeneric<float>.TestMethod3;
        }
    }

    [BurstCompile]
    private struct MyGenericJobSeparateAssembly<T> : BurstReflectionTestsSeparateAssembly.IMyGenericJobSeparateAssembly<T>
    {
        public void Execute() { }

        private static void UseConcreteType()
        {
            new MyGenericJobSeparateAssembly<int>().Execute();
        }
    }

    [Test]
    [TestCase("BurstReflectionTests.GenericMethodContainer.GenericMethod(T)")]
    public void ExcludesGenericMethods(string compileTargetName)
    {
        var result = BurstReflection.FindExecuteMethods(_assemblies, BurstReflectionAssemblyOptions.None);

        Assert.That(result.LogMessages, Is.Empty);

        var compileTarget = result.CompileTargets.Find(x => x.GetDisplayName() == compileTargetName);
        Assert.That(compileTarget, Is.Null);
    }

    [BurstCompile]
    private static class GenericMethodContainer
    {
        [BurstCompile]
        private static void GenericMethod<T>(T p)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class RegisterGenericJobTypeAttribute : Attribute
    {
        public Type ConcreteType;

        public RegisterGenericJobTypeAttribute(Type type)
        {
            ConcreteType = type;
        }
    }

    [BurstCompile]
    internal struct GenericParallelForJob<T> : IJobParallelFor
        where T : struct
    {
        public void Execute(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
