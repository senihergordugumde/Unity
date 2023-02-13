using System;
using static Unity.Burst.CompilerServices.Hint;

namespace Burst.Compiler.IL.Tests
{
    internal class Hint
    {
        [TestCompiler(42)]
        public static unsafe double CheckLikely(int val)
        {
            if (Likely(val < 42))
            {
                return Math.Pow(Math.Tan(val), 42.42);
            }
            else
            {
                return Math.Cos(val);
            }
        }

        [TestCompiler(42)]
        public static unsafe double CheckUnlikely(int val)
        {
            if (Unlikely(val < 42))
            {
                return Math.Pow(Math.Tan(val), 42.42);
            }
            else
            {
                return Math.Cos(val);
            }
        }

        [TestCompiler(42)]
        public static unsafe double CheckAssume(int val)
        {
            Assume(val >= 42);

            if (val < 42)
            {
                return Math.Pow(Math.Tan(val), 42.42);
            }
            else
            {
                return Math.Cos(val);
            }
        }

        [TestCompiler(0)]
        [TestCompiler(1)]
        public static int CheckLikelyMatches(int val)
        {
            var cond = val == 0;
            return cond == Likely(cond) ? 1 : 0;
        }

        [TestCompiler(0)]
        [TestCompiler(1)]
        public static int CheckUnlikelyMatches(int val)
        {
            var cond = val == 0;
            return cond == Unlikely(cond) ? 1 : 0;
        }
    }
}
