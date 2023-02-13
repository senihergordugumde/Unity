using System;

namespace Burst.Compiler.IL.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
#if BURST_INTERNAL
    public
#else
    internal
#endif
    class MonoOnlyAttribute : Attribute
    {
#pragma warning disable CS0414
        public MonoOnlyAttribute(string reason)
        {
        }
#pragma warning restore CS0414

    }
}
