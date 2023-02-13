using System;

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Can be used to specify that a warning produced by Burst for a given
    /// method should be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class IgnoreWarningAttribute : Attribute
    {
        /// <summary>
        /// Ignore a single warning produced by Burst.
        /// </summary>
        /// <param name="warning">The warning to ignore.</param>
        public IgnoreWarningAttribute(int warning) { }
    }
}
