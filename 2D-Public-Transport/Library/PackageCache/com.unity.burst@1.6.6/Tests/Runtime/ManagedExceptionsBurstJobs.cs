using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
using System.Runtime.CompilerServices;

#if UNITY_2019_4_OR_NEWER
namespace ExceptionsFromBurstJobs
{
    [BurstCompile]
    class ManagedExceptionsBurstJobs
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewArgumentException()
        {
            throw new ArgumentException("A");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowArgumentExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewArgumentException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowArgumentException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException: A"));

            var jobData = new ThrowArgumentExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewArgumentNullException()
        {
            throw new ArgumentNullException("N");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowArgumentNullExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewArgumentNullException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowArgumentNullException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("System.ArgumentNullException: N"));

            var jobData = new ThrowArgumentNullExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewNullReferenceException()
        {
            throw new NullReferenceException("N");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowNullReferenceExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewNullReferenceException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowNullReferenceException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException: N"));

            var jobData = new ThrowNullReferenceExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewInvalidOperationException()
        {
            throw new InvalidOperationException("IO");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowInvalidOperationExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewInvalidOperationException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowInvalidOperationException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: IO"));

            var jobData = new ThrowInvalidOperationExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewNotSupportedException()
        {
            throw new NotSupportedException("NS");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowNotSupportedExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewNotSupportedException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowNotSupportedException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("NotSupportedException: NS"));

            var jobData = new ThrowNotSupportedExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewUnityException()
        {
            throw new UnityException("UE");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowUnityExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewUnityException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowUnityException()
        {
            LogAssert.Expect(LogType.Exception, new Regex("UnityException: UE"));

            var jobData = new ThrowUnityExceptionJob();
            jobData.Run();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowNewIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException("IOOR");
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowIndexOutOfRangeExceptionJob : IJob
        {
            public void Execute()
            {
                ThrowNewIndexOutOfRangeException();
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowIndexOutOfRange()
        {
            LogAssert.Expect(LogType.Exception, new Regex("IndexOutOfRangeException: IOOR"));

            var jobData = new ThrowIndexOutOfRangeExceptionJob();
            jobData.Run();
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct ThrowFromDereferenceNullJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Ptr;

            public void Execute()
            {
                *Ptr = 42;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowFromDereferenceNull()
        {
            LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException: Object reference not set to an instance of an object"));

            var jobData = new ThrowFromDereferenceNullJob() { Ptr = null };
            jobData.Run();
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct ThrowFromDivideByZeroJob : IJob
        {
            public int Int;

            public void Execute()
            {
                Int = 42 / Int;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowFromDivideByZero()
        {
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                // Arm64 does not throw a divide-by-zero exception, instead it flushes the result to zero.
                return;
            }

            LogAssert.Expect(LogType.Exception, new Regex("DivideByZeroException: Attempted to divide by zero"));

            var jobData = new ThrowFromDivideByZeroJob() { Int = 0 };
            jobData.Run();
        }

        private unsafe delegate void ExceptionDelegate(int* a);

        [BurstCompile(CompileSynchronously = true)]
        private static unsafe void DereferenceNull(int* a)
        {
            *a = 42;
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct ThrowFromFunctionPointerJob : IJob
        {
#pragma warning disable 649
            [NativeDisableUnsafePtrRestriction] public IntPtr FuncPtr;
            [NativeDisableUnsafePtrRestriction] public int* Ptr;
#pragma warning restore 649

            public void Execute()
            {
                new FunctionPointer<ExceptionDelegate>(FuncPtr).Invoke(Ptr);

                // Set Ptr to non null which should never be hit because the above will throw.
                Ptr = (int*)0x42;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public unsafe void ThrowFromFunctionPointer()
        {
            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(DereferenceNull);
            LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException: Object reference not set to an instance of an object"));
            var job = new ThrowFromFunctionPointerJob { FuncPtr = funcPtr.Value, Ptr = null };
            job.Run();
            Assert.AreEqual((IntPtr)job.Ptr, (IntPtr)0);
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct ThrowFromDereferenceNullParallelJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Ptr;

            public void Execute(int index)
            {
                *Ptr = index;
            }
        }

        [Test]
        // No RuntimePlatform.OSXEditor in this list because of a subtle Mojave only bug.
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowFromDereferenceNullParallel()
        {
            var messageCount = 0;

            void OnMessage(string message, string stackTrace, LogType type)
            {
                Assert.AreEqual(LogType.Exception, type);
                StringAssert.Contains("NullReferenceException: Object reference not set to an instance of an object", message);
                messageCount++;
            }

            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceivedThreaded += OnMessage;

            try
            {
                var jobData = new ThrowFromDereferenceNullParallelJob() { Ptr = null };
                jobData.Schedule(128, 1).Complete();

                Assert.GreaterOrEqual(messageCount, 1);
            }
            finally
            {
                Application.logMessageReceivedThreaded -= OnMessage;
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private unsafe struct ThrowFromDereferenceNullManagedJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Ptr;

            public void Execute()
            {
                *Ptr = 42;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowFromDereferenceNullManaged()
        {
            LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException: Object reference not set to an instance of an object"));

            var jobData = new ThrowFromDereferenceNullManagedJob() { Ptr = null };
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void ThrowFromDereferenceNullBurstDisabled()
        {
            var previous = BurstCompiler.Options.EnableBurstCompilation;
            BurstCompiler.Options.EnableBurstCompilation = false;

            LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException: Object reference not set to an instance of an object"));

            var jobData = new ThrowFromDereferenceNullJob() { Ptr = null };
            jobData.Run();

            BurstCompiler.Options.EnableBurstCompilation = previous;
        }

        private unsafe struct ThrowFromManagedStackOverflowJob : IJob
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private static int DoStackOverflow(ref int x)
            {
                // Copy just to make the stack grow.
                var copy = x;
                return copy + DoStackOverflow(ref x);
            }

            public int Int;

            public void Execute()
            {
                Int = DoStackOverflow(ref Int);
            }
        }

        //[Test]
        //[UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        public void ThrowFromManagedStackOverflow()
        {
            LogAssert.Expect(LogType.Exception, new Regex("StackOverflowException: The requested operation caused a stack overflow"));

            var jobData = new ThrowFromManagedStackOverflowJob() { Int = 1 };
            jobData.Run();
        }
    }
}
#endif // #if UNITY_2019_3_OR_NEWER
