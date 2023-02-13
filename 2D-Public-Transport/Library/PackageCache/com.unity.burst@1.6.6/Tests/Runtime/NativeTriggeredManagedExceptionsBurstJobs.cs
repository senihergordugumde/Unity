using NUnit.Framework;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_2019_3_OR_NEWER
namespace ExceptionsFromBurstJobs
{
    class NativeTriggeredManagedExceptionsBurstJobs
    {
        [BurstCompile(CompileSynchronously = true)]
        struct RaiseMonoExceptionJob : IJob
        {
            public float output;
            public void Execute()
            {
                output = Time.deltaTime;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void RaiseMonoException()
        {
            var job = new RaiseMonoExceptionJob();
            LogAssert.Expect(LogType.Exception, new Regex(
                "UnityEngine::UnityException: get_deltaTime can only be called from the main thread." + "[\\s]*" +
                "Constructors and field initializers will be executed from the loading thread when loading a scene." + "[\\s]*" +
                "Don't use this function in the constructor or field initializers, instead move initialization code to the Awake or Start function." + "[\\s]*" +
                "This Exception was thrown from a job compiled with Burst, which has limited exception support."
                ));
            job.Run();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct RaiseInvalidOperationExceptionJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> test;
            public void Execute()
            {
                test[0] = 5;
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        public void RaiseInvalidOperationException()
        {
            var jobData = new RaiseInvalidOperationExceptionJob();
            var testArray = new NativeArray<int>(1, Allocator.Persistent);
            jobData.test = testArray;

            LogAssert.Expect(LogType.Exception, new Regex(
                "System::InvalidOperationException: The .+ has been declared as \\[ReadOnly\\] in the job( .+)?, but you are writing to it\\." + "[\\s]*" +
                "This Exception was thrown from a job compiled with Burst, which has limited exception support."
                ));
            jobData.Run();
            testArray.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct RaiseArgumentNullExceptionJob : IJob
        {
#pragma warning disable 649
            [NativeDisableUnsafePtrRestriction] public void* dst;
#pragma warning restore 649
            public void Execute()
            {
                UnsafeUtility.MemCpy(dst, null, 10);
            }
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        [Description("Requires ENABLE_UNITY_COLLECTIONS_CHECKS which is currently only enabled in the Editor")]
        unsafe public void RaiseArgumentNullException()
        {
            var jobData = new RaiseArgumentNullExceptionJob();
            jobData.dst = UnsafeUtility.Malloc(10, 4, Allocator.Temp);
            LogAssert.Expect(LogType.Exception, new Regex(
                "System.ArgumentNullException: source" + "[\\s]*" +
                "This Exception was thrown from a job compiled with Burst, which has limited exception support."
                ));
            jobData.Run();
            UnsafeUtility.Free(jobData.dst, Allocator.Temp);
        }
    }
}
#endif // #if UNITY_2019_3_OR_NEWER
