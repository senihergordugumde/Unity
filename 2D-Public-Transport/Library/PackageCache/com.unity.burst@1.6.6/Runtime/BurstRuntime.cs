using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.Burst
{
    /// <summary>
    /// Provides helper intrinsics that can be used at runtime.
    /// </summary>
#if BURST_COMPILER_SHARED
    internal static class BurstRuntimeInternal
#else
    public static class BurstRuntime
#endif
    {
        /// <summary>
        /// Gets a 32-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// </summary>
        /// <typeparam name="T">The type to compute the hash from</typeparam>
        /// <returns>The 32-bit hashcode.</returns>
        public static int GetHashCode32<T>()
        {
#if !UNITY_DOTSRUNTIME_IL2CPP
            return HashCode32<T>.Value;
#else
            // DOTS Runtime IL2CPP Builds do not use C#'s lazy static initialization order (it uses a C like order, aka random)
            // As such we cannot rely on static init for caching types since any static constructor calling this function
            // may return uninitialized/default-initialized memory
            return HashStringWithFNV1A32(typeof(T).AssemblyQualifiedName);
#endif
        }

        /// <summary>
        /// Gets a 32-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// This method cannot be used from a burst job.
        /// </summary>
        /// <param name="type">The type to compute the hash from</param>
        /// <returns>The 32-bit hashcode.</returns>
        public static int GetHashCode32(Type type)
        {
            return HashStringWithFNV1A32(type.AssemblyQualifiedName);
        }

        /// <summary>
        /// Gets a 64-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// </summary>
        /// <typeparam name="T">The type to compute the hash from</typeparam>
        /// <returns>The 64-bit hashcode.</returns>
        public static long GetHashCode64<T>()
        {
#if !UNITY_DOTSRUNTIME_IL2CPP
            return HashCode64<T>.Value;
#else
            // DOTS Runtime IL2CPP Builds do not use C#'s lazy static initialization order (it uses a C like order, aka random)
            // As such we cannot rely on static init for caching types since any static constructor calling this function
            // may return uninitialized/default-initialized memory
            return HashStringWithFNV1A64(typeof(T).AssemblyQualifiedName);
#endif
        }

        /// <summary>
        /// Gets a 64-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>.
        /// This method cannot be used from a burst job.
        /// </summary>
        /// <param name="type">Type to calculate a hash for</param>
        /// <returns>The 64-bit hashcode.</returns>
        public static long GetHashCode64(Type type)
        {
            return HashStringWithFNV1A64(type.AssemblyQualifiedName);
        }

        // method internal as it is used by the compiler directly
        internal static int HashStringWithFNV1A32(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            uint result = offsetBasis;
            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }
            return (int)result;
        }

        // method internal as it is used by the compiler directly
        internal static long HashStringWithFNV1A64(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong result = offsetBasis;
            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }
            return (long)result;
        }

        private struct HashCode32<T>
        {
            public static readonly int Value = HashStringWithFNV1A32(typeof(T).AssemblyQualifiedName);
        }

        private struct HashCode64<T>
        {
            public static readonly long Value = HashStringWithFNV1A64(typeof(T).AssemblyQualifiedName);
        }

#if !BURST_COMPILER_SHARED

        /// <summary>
        /// Allows for loading additional Burst native libraries
        /// Important: Designed for Play mode / Desktop Standalone Players ONLY
        /// In Editor, any libraries that have been loaded will be unloaded on exit of playmode
        /// Only supported from 2020.1 and later. You can use BurstCompiler.IsLoadAdditionalLibrarySupported() to confirm it is available.
        /// </summary>
        /// <param name="pathToLibBurstGenerated">Absolute filesystem location of bursted library to load</param>
        /// <returns>true if the library was loaded successfully</returns>
        public static bool LoadAdditionalLibrary(string pathToLibBurstGenerated)
        {
            if (BurstCompiler.IsLoadAdditionalLibrarySupported())
            {
                return LoadAdditionalLibraryInternal(pathToLibBurstGenerated);
            }
            return false;
        }

        internal static bool LoadAdditionalLibraryInternal(string pathToLibBurstGenerated)
        {
#if !UNITY_DOTSRUNTIME
            return (bool)typeof(Unity.Burst.LowLevel.BurstCompilerService).GetMethod("LoadBurstLibrary").Invoke(null, new object[] { pathToLibBurstGenerated });
#else
            return false;
#endif
        }


#if UNITY_2020_1_OR_NEWER
        internal static void Initialize()
        {
        }

        // Prevent BurstCompilerService.Log from being stripped, introduce PreserveAttribute to avoid
        //requiring a unityengine using directive, il2cpp will see the attribute and know to not strip
        //the Log method and its BurstCompilerService.Log dependency
        internal class PreserveAttribute : System.Attribute {}

        [Preserve]
        internal static unsafe void Log(byte* message, int logType, byte* fileName, int lineNumber)
        {
            Unity.Burst.LowLevel.BurstCompilerService.Log((byte*) 0, (Unity.Burst.LowLevel.BurstCompilerService.BurstLogType)logType, message, fileName, lineNumber);
        }
#elif UNITY_2019_4_OR_NEWER
        // Because we can't back-port the new API BurstCompilerService.Log introduced in 2020.1
        // we are still trying to allow to log on earlier version of Unity by going back to managed
        // code when we are using Debug.Log. It is not great in terms of performance but it should not
        // be a matter when debugging.

        internal static unsafe void Log(byte* message, int logType, byte* fileName, int lineNumber)
        {
            var fp = LogHelper.Instance.Data;
            // If we have a domain reload, the function pointer will be cleared, so we can't call it.
            if (fp.IsCreated)
            {
                fp.Invoke(message, logType, fileName, lineNumber);
            }
        }

        private unsafe delegate void NativeLogDelegate(byte* message, int logType, byte* filename, int lineNumber);

        private static readonly unsafe NativeLogDelegate ManagedNativeLog = ManagedNativeLogImpl;

        [AOT.MonoPInvokeCallback(typeof(NativeLogDelegate))]
        private static unsafe void ManagedNativeLogImpl(byte* message, int logType, byte* filename, int lineNumber)
        {
            if (message == null) return;
            int byteCount = 0;
            while (message[byteCount] != 0) byteCount++;

            var managedText = Encoding.UTF8.GetString(message, byteCount);
            switch (logType)
            {
                case 1:
                    UnityEngine.Debug.LogWarning(managedText);
                    break;
                case 2:
                    UnityEngine.Debug.LogError(managedText);
                    break;
                default:
                    UnityEngine.Debug.Log(managedText);
                    break;
            }
        }

        private class LogHelper
        {
            public static readonly SharedStatic<FunctionPointer<NativeLogDelegate>> Instance = SharedStatic<FunctionPointer<NativeLogDelegate>>.GetOrCreate<LogHelper>();
        }

        static BurstRuntime()
        {
            LogHelper.Instance.Data = new FunctionPointer<NativeLogDelegate>(Marshal.GetFunctionPointerForDelegate(ManagedNativeLog));
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
        }
#else
        internal static void Initialize()
        {
        }

        internal static unsafe void Log(byte* message, int logType, byte* fileName, int lineNumber)
        {
        }
#endif

#endif // !BURST_COMPILER_SHARED


        /// <summary>
        /// Return a pointer to read-only memory consisting of the literal UTF-8 bytes of a string constant.
        /// </summary>
        /// <param name="str">A string which must a string literal</param>
        /// <param name="byteCount">Receives the number of UTF-8 encoded bytes the constant contains (excluding null terminator)</param>
        /// <returns>A pointer to constant data representing the UTF-8 encoded bytes of the string literal, terminated with a null terminator</returns>
        public unsafe static byte* GetUTF8LiteralPointer(string str, out int byteCount)
        {
            throw new NotImplementedException("This function only works from Burst");
        }

    }
}
