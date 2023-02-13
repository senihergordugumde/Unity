#if UNITY_ANDROID
using System.Runtime.InteropServices;

namespace Burst.Compiler.IL.Tests
{
    public class DllImportAndroid
    {
        public unsafe struct HandleStruct
        {
            public void* Handle;
        }

        public struct NestedHandleStruct
        {
            public HandleStruct Handle;
        }

        public unsafe struct TypedHandleStruct
        {
            public byte* Handle;
        }

        public struct IntInStruct
        {
            public int Handle;
        }

        public struct LongInStruct
        {
            public long Handle;
        }

        [DllImport("burst-dllimport-native")]
        public static extern void allVoid();

        [TestCompiler]
        public static void AllVoid()
        {
            allVoid();
        }

        [DllImport("burst-dllimport-native")]
        public static extern int incrementByOne(int x);

        [TestCompiler]
        public static int UseDllImportedFunction()
        {
            return incrementByOne(41);
        }

        [DllImport("burst-dllimport-native")]
        public static extern int readFromPtr(ref int x);

        [TestCompiler]
        public static int ReadFromPtr()
        {
            int x = 37;
            return readFromPtr(ref x);
        }

        [DllImport("burst-dllimport-native")]
        public static extern HandleStruct handleStruct(HandleStruct handle);

        [TestCompiler]
        public unsafe static long HandleStructByVal()
        {
            var handle = new HandleStruct { Handle = (void*)0x42 };
            return (long)handleStruct(handle).Handle;
        }

        [DllImport("burst-dllimport-native")]
        public static extern NestedHandleStruct nestedHandleStruct(NestedHandleStruct handle);

        [TestCompiler]
        public unsafe static long NestedHandleStructByVal()
        {
            var handle = new NestedHandleStruct { Handle = new HandleStruct { Handle = (void*)0x42 } };
            return (long)nestedHandleStruct(handle).Handle.Handle;
        }

        [DllImport("burst-dllimport-native")]
        public static extern TypedHandleStruct typedHandleStruct(TypedHandleStruct handle);

        [TestCompiler]
        public unsafe static long TypedHandleStructByVal()
        {
            var handle = new TypedHandleStruct { Handle = (byte*)0x42 };
            return (long)typedHandleStruct(handle).Handle;
        }

        [DllImport("burst-dllimport-native")]
        public static extern IntInStruct intInStruct(IntInStruct handle);

        [TestCompiler]
        public unsafe static long IntInStructByVal()
        {
            var handle = new IntInStruct { Handle = 0x42424242 };
            return (long)intInStruct(handle).Handle;
        }

        [DllImport("burst-dllimport-native")]
        public static extern LongInStruct longInStruct(LongInStruct handle);

        [TestCompiler]
        public unsafe static long LongInStructByVal()
        {
            var handle = new LongInStruct { Handle = 0x4242424242424242 };
            return (long)longInStruct(handle).Handle;
        }
    }
}
#endif