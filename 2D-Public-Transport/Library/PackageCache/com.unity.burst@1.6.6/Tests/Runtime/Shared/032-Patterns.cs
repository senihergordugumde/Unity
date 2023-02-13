// These tests require 2020.2 because that enabled C# 8.0.
#if BURST_TESTS_ONLY || UNITY_2020_2_OR_NEWER
namespace Burst.Compiler.IL.Tests.Shared
{
    public class Patterns
    {
        [TestCompiler(2)]
        [TestCompiler(1)]
        [TestCompiler(0)]
        public static int PropertyPattern(int x)
        {
            var point = new Point { X = x, Y = 5 };

            return point switch
            {
                { X: 2 } => 10,
                { X: 1 } => 5,
                _ => 0
            };
        }

        private struct Point
        {
            public int X;
            public int Y;
        }

        [TestCompiler(1, 2)]
        [TestCompiler(2, 4)]
        [TestCompiler(0, 0)]
        public static int TuplePattern(int x, int y)
        {
            return (x, y) switch
            {
                (1, 2) => 10,
                (2, 4) => 5,
                _ => 0
            };
        }

        private struct DeconstructablePoint
        {
            public int X;
            public int Y;

            public void Deconstruct(out int x, out int y) => (x, y) = (X, Y);
        }

        [TestCompiler(1, -1)]
        [TestCompiler(-1, 1)]
        [TestCompiler(1, 1)]
        [TestCompiler(-1, -1)]
        public static int PositionalPattern(int pointX, int pointY)
        {
            var point = new DeconstructablePoint { X = pointX, Y = pointY };

            return point switch
            {
                (0, 0) => 0,
                var (x, y) when x > 0 && y > 0 => 1,
                var (x, y) when x < 0 && y > 0 => 2,
                var (x, y) when x < 0 && y < 0 => 3,
                var (x, y) when x > 0 && y < 0 => 4,
                var (_, _) => 5
            };
        }
    }
}
#endif