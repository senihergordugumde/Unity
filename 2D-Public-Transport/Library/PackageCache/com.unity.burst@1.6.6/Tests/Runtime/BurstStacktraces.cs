using System;
using AOT;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_2021_1_OR_NEWER
namespace BurstStacktraces
{
    [BurstCompile]
    class BurstStacktracesJobs
    {
        [BurstCompile(CompileSynchronously = true, Debug = true)]
        struct ThrowManagedExceptionDebugJob : IJob
        {
            public void Execute()
            {
                throw new ArgumentException("A");
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct ThrowManagedExceptionJob : IJob
        {
            public void Execute()
            {
                throw new ArgumentException("A");
            }
        }

        [BurstCompile(CompileSynchronously = true, Debug = true)]
        unsafe struct ThrowNativeExceptionDebugJob : IJob
        {
#pragma warning disable 649
            [NativeDisableUnsafePtrRestriction] public int* Ptr;
#pragma warning restore 649

            public void Execute()
            {
                // Ptr is null, so this will fail with a null reference exception.
                *Ptr = 42;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct ThrowNativeExceptionJob : IJob
        {
#pragma warning disable 649
            [NativeDisableUnsafePtrRestriction] public int* Ptr;
#pragma warning restore 649

            public void Execute()
            {
                // Ptr is null, so this will fail with a null reference exception.
                *Ptr = 42;
            }
        }

        private delegate int ExceptionDelegate(int a);

        [BurstCompile(CompileSynchronously = true)]
        [MonoPInvokeCallback(typeof(ExceptionDelegate))]
        private static int ThrowManagedExceptionFunctionPointer(int a)
        {
            throw new ArgumentException("A");
        }

        [BurstCompile(CompileSynchronously = true)]
        [MonoPInvokeCallback(typeof(ExceptionDelegate))]
        private static int ThrowNativeExceptionFunctionPointer(int a)
        {
            // a is zero so this will fail with a divide by zero native hardware exception.
            return 42 / a;
        }

        [BurstCompile(CompileSynchronously = true, Debug = true)]
        [MonoPInvokeCallback(typeof(ExceptionDelegate))]
        private static int ThrowManagedExceptionDebugFunctionPointer(int a)
        {
            throw new ArgumentException("A");
        }

        [BurstCompile(CompileSynchronously = true, Debug = true)]
        [MonoPInvokeCallback(typeof(ExceptionDelegate))]
        private static int ThrowNativeExceptionDebugFunctionPointer(int a)
        {
            // a is zero so this will fail with a divide by zero native hardware exception.
            return 42 / a;
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Description("Only Windows has built-in support for line numbers in stack traces")]
        public void ThrowManagedExceptionDebugWindowsFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

#if UNITY_2021_1_OR_NEWER
            // The line location was fixed in 2021.1+
            LogAssert.Expect(LogType.Exception, new Regex("\\[BurstStacktraces\\.cs:22\\]"));
#else
            LogAssert.Expect(LogType.Exception, new Regex("\\[BurstStacktraces\\.cs:21\\]"));
#endif

            var jobData = new ThrowManagedExceptionDebugJob();
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Description("Only Windows has built-in support for line numbers in stack traces")]
        public void ThrowNativeExceptionDebugWindowsFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

#if UNITY_2021_1_OR_NEWER
            // The line location was fixed in 2021.1+
            LogAssert.Expect(LogType.Exception, new Regex("\\[BurstStacktraces\\.cs:45\\]"));
#else
            LogAssert.Expect(LogType.Exception, new Regex("\\[BurstStacktraces\\.cs:44\\]"));
#endif

            var jobData = new ThrowNativeExceptionDebugJob { Ptr = null };
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowManagedExceptionDebugFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            LogAssert.Expect(LogType.Exception, new Regex("BurstStacktraces\\.BurstStacktracesJobs\\.ThrowManagedExceptionDebugJob"));

            var jobData = new ThrowManagedExceptionDebugJob();
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowManagedExceptionFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = false;

            LogAssert.Expect(LogType.Exception, new Regex("Unity\\.Jobs\\.IJobExtensions\\.JobStruct`1<BurstStacktraces\\.BurstStacktracesJobs\\.ThrowManagedExceptionJob>\\.Execute"));

            var jobData = new ThrowManagedExceptionJob();
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowNativeExceptionDebugFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            LogAssert.Expect(LogType.Exception, new Regex("BurstStacktraces\\.BurstStacktracesJobs\\.ThrowNativeExceptionDebugJob"));

            var jobData = new ThrowNativeExceptionDebugJob { Ptr = null };
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowNativeExceptionFromJob()
        {
            BurstCompiler.Options.EnableBurstDebug = false;

            LogAssert.Expect(LogType.Exception, new Regex("Unity\\.Jobs\\.IJobExtensions\\.JobStruct`1<BurstStacktraces\\.BurstStacktracesJobs\\.ThrowNativeExceptionJob>\\.Execute"));

            var jobData = new ThrowNativeExceptionJob { Ptr = null };
            jobData.Run();
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Description("Only Windows has built-in support for line numbers in stack traces")]
        public void ThrowManagedExceptionDebugWindowsFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowManagedExceptionDebugFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
#if UNITY_2021_1_OR_NEWER
                // The line location was fixed in 2021.1+
                Assert.IsTrue(e.Message.Contains("[BurstStacktraces.cs:84] BurstStacktraces.BurstStacktracesJobs.ThrowManagedExceptionDebugFunctionPointer"));
#else
                Assert.IsTrue(e.Message.Contains("[BurstStacktraces.cs:83] BurstStacktraces.BurstStacktracesJobs.ThrowManagedExceptionDebugFunctionPointer"));
#endif
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Description("Only Windows has built-in support for line numbers in stack traces")]
        public void ThrowNativeExceptionDebugWindowsFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowNativeExceptionDebugFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
#if UNITY_2021_1_OR_NEWER
                // The line location was fixed in 2021.1+
                Assert.IsTrue(e.Message.Contains("[BurstStacktraces.cs:92] BurstStacktraces.BurstStacktracesJobs.ThrowNativeExceptionDebugFunctionPointer"));
#else
                Assert.IsTrue(e.Message.Contains("[BurstStacktraces.cs:91] BurstStacktraces.BurstStacktracesJobs.ThrowNativeExceptionDebugFunctionPointer"));
#endif
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowManagedExceptionDebugFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowManagedExceptionDebugFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("BurstStacktraces.BurstStacktracesJobs.ThrowManagedExceptionDebugFunctionPointer"));
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowManagedExceptionFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = false;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowManagedExceptionFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("BurstStacktraces.BurstStacktracesJobs.ThrowManagedExceptionFunctionPointer"));
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowNativeExceptionDebugFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = true;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowNativeExceptionDebugFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("BurstStacktraces.BurstStacktracesJobs.ThrowNativeExceptionDebugFunctionPointer"));
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        [Description("Requires stacktrace exception support which is only supported in the Editor")]
        public void ThrowNativeExceptionFromFunctionPointer()
        {
            BurstCompiler.Options.EnableBurstDebug = false;

            var funcPtr = BurstCompiler.CompileFunctionPointer<ExceptionDelegate>(ThrowNativeExceptionFunctionPointer);

            try
            {
                funcPtr.Invoke(0);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("BurstStacktraces.BurstStacktracesJobs.ThrowNativeExceptionFunctionPointer"));
            }
        }
    }
}
#endif
