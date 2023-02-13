using System;
using Unity.Burst;
using UnityBenchShared;

namespace Burst.Compiler.IL.Tests
{
    /// <summary>
    /// Tests types
    /// </summary>
    [BurstCompile]
    internal class SystemGuid
    {
        public struct SystemGuidProvider : IArgumentProvider
        {
            public object Value => new Guid(0x26b6afc2, 0xf1b2, 0x479d, 0xb2, 0xad, 0x13, 0x2f, 0x17, 0x8d, 0x3a, 0xe0);
        }

        // TODO: Gold disabled because System.Guid has very different code on Mono/macOS vs .NET/Windows. Should re-check
        // once we use .NET everywhere.
        [TestCompiler(typeof(SystemGuidProvider), DisableGold = true)]
        public static int GuidArg(ref Guid guid)
        {
            return guid == new Guid(0x26b6afc2, 0xf1b2, 0x479d, 0xb2, 0xad, 0x13, 0x2f, 0x17, 0x8d, 0x3a, 0xe0) ? 1 : 0;
        }

        public static readonly Guid StaticReadonlyGuid = new Guid(0x26b6afc2, 0xf1b2, 0x479d, 0xb2, 0xad, 0x13, 0x2f, 0x17, 0x8d, 0x3a, 0xe0);

        [TestCompiler]
        public static int GuidStaticReadonly()
        {
            return StaticReadonlyGuid == new Guid(0x26b6afc2, 0xf1b2, 0x479d, 0xb2, 0xad, 0x13, 0x2f, 0x17, 0x8d, 0x3a, 0xe0) ? 1 : 0;
        }
    }
}
