public class BurstReflectionTestsSeparateAssembly
{
    [Unity.Jobs.LowLevel.Unsafe.JobProducerType(typeof(MyJobProducerSeparateAssembly<,>))]
    public interface IMyGenericJobSeparateAssembly<T>
    {
        void Execute();
    }

    private static class MyJobProducerSeparateAssembly<TJob, T>
    {
        public static void Execute(ref TJob job)
        {

        }
    }
}
