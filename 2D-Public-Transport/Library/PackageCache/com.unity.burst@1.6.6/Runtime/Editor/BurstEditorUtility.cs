#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Provides helper methods that can be used in the Editor.
    /// </summary>
    internal static class BurstEditorUtility
    {
#if UNITY_2020_1_OR_NEWER
        /// <summary>
        /// Requests previously-compiled functions to be cleared from the cache during the next domain reload.
        /// Note that this method does not trigger a domain reload itself, so it should be paired with
        /// <see cref="EditorUtility.RequestScriptReload()"/> to force a domain reload.
        /// </summary>
        /// <remarks>
        /// During the next domain reload, previously-compiled functions are unloaded from memory,
        /// and the corresponding libraries in the on-disk cache are deleted.
        ///
        /// This method cannot be called while the Editor is in play mode.
        /// </remarks>
        /// <example>
        /// The following example shows calling this method in a test, then triggering a domain reload,
        /// and then waiting for the domain reload to finish:
        /// <code>
        /// BurstEditorUtility.RequestClearJitCache();
        /// EditorUtility.RequestScriptReload();
        /// yield return new WaitForDomainReload();
        /// </code>
        /// </example>
        public static void RequestClearJitCache()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("This method cannot be called while the Editor is in play mode");
            }

            BurstCompiler.RequestClearJitCache();
        }
#endif
    }
}
#endif