using Burst.Compiler.IL.Tests.Helpers;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    internal partial class VectorsConstructors
    {
        // ---------------------------------------------------
        // float4
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static float Float4Int(int a)
        {
            return Vectors.ConvertToFloat(new float4(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Float3Float(float x)
        {
            return Vectors.ConvertToFloat(new float4(new float3(x), 5.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Float2Float2(float x)
        {
            return Vectors.ConvertToFloat(new float4(new float2(x), new float2(5.0f)));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float44Floats(float a)
        {
            return Vectors.ConvertToFloat(new float4(1.0f, 2.0f, 3.0f + a, 4.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Float(float a)
        {
            return Vectors.ConvertToFloat(new float4(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Int4(ref int4 a)
        {
            return Vectors.ConvertToFloat((float4) new float4(a).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Half(float a)
        {
            var h = new half(a);
            return Vectors.ConvertToFloat((float4)new float4(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4Half4(ref float4 a)
        {
            var h = new half4(a);
            return Vectors.ConvertToFloat((float4)new float4(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4HalfExplicit(float a)
        {
            return Vectors.ConvertToFloat((float4) new half(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float4HalfImplicit(float a)
        {
            float4 x =new half(a);
            return Vectors.ConvertToFloat(x);
        }

        // ---------------------------------------------------
        // float3
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static float Float3Int(int a)
        {
            return Vectors.ConvertToFloat(new float3(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float33Floats(float a)
        {
            return Vectors.ConvertToFloat(new float3(1.0f, 2.0f, 3.0f + a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float3Float(float a)
        {
            return Vectors.ConvertToFloat(new float3(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float3Float2Float(float a)
        {
            return Vectors.ConvertToFloat(new float3(new float2(a), 5.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float3Half(float a)
        {
            var h = new half(a);
            return Vectors.ConvertToFloat((float3)new float3(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float3Half3(ref float3 a)
        {
            var h = new half3(a);
            return Vectors.ConvertToFloat((float3)new float3(h).x);
        }

        // ---------------------------------------------------
        // float2
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static float Float2Int(int a)
        {
            return Vectors.ConvertToFloat(new float2(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float22Floats(float a)
        {
            return Vectors.ConvertToFloat(new float2(1.0f, 3.0f + a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float2Float(float a)
        {
            return Vectors.ConvertToFloat(new float2(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float2Half(float a)
        {
            var h = new half(a);
            return Vectors.ConvertToFloat((float2)new float2(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static float Float2Half2(ref float2 a)
        {
            var h = new half2(a);
            return Vectors.ConvertToFloat((float2)new float2(h).x);
        }

        // ---------------------------------------------------
        // int4
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static int Int4Int(int a)
        {
            return Vectors.ConvertToInt(new int4(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static int Int4Int3Int(int x)
        {
            return Vectors.ConvertToInt(new int4(new int3(x), 5));
        }

        [TestCompiler(DataRange.Standard)]
        public static int Int44Ints(int a)
        {
            return Vectors.ConvertToInt(new int4(1, 2, 3 + a, 4));
        }

        // ---------------------------------------------------
        // int3
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static int Int3Int(int a)
        {
            return Vectors.ConvertToInt(new int3(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static int Int33Ints(int a)
        {
            return Vectors.ConvertToInt(new int3(1, 2, 3 + a));
        }

        [TestCompiler(DataRange.Standard)]
        public static int Int3Int2Int(int a)
        {
            return Vectors.ConvertToInt(new int3(new int2(a), 5));
        }

        // ---------------------------------------------------
        // int2
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static int Int2Int(int a)
        {
            return Vectors.ConvertToInt(new int2(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static int Int22Ints(int a)
        {
            return Vectors.ConvertToInt(new int2(1, 3 + a));
        }


        // ---------------------------------------------------
        // bool4
        // ---------------------------------------------------

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static int Bool4(bool a)
        {
            return Vectors.ConvertToInt(new bool4(a));
        }

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static int Bool4Bool3(bool x)
        {
            return Vectors.ConvertToInt(new bool4(new bool3(x), true));
        }

        [TestCompiler(false, false, false, false)]
        [TestCompiler(true, false, false, false)]
        [TestCompiler(false, true, false, false)]
        [TestCompiler(false, false, true, false)]
        [TestCompiler(false, false, false, true)]
        public static int Bool44Bools(bool a, bool b, bool c, bool d)
        {
            return Vectors.ConvertToInt(new bool4(a, b, c, d));
        }

        // ---------------------------------------------------
        // bool3
        // ---------------------------------------------------

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static int Bool3(bool a)
        {
            return Vectors.ConvertToInt(new bool3(a));
        }

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static int Bool3Bool2(bool a)
        {
            return Vectors.ConvertToInt(new bool3(new bool2(a), true));
        }

        [TestCompiler(false, false, false)]
        [TestCompiler(true, false, false)]
        [TestCompiler(false, true, false)]
        [TestCompiler(false, false, true)]
        public static int Bool33Bools(bool a, bool b, bool c)
        {
            return Vectors.ConvertToInt(new bool3(a, b, c));
        }

        // ---------------------------------------------------
        // bool2
        // ---------------------------------------------------

        [TestCompiler(true)]
        [TestCompiler(false)]
        public static int Bool2(bool a)
        {
            return Vectors.ConvertToInt(new bool2(a));
        }

        [TestCompiler(true, false)]
        [TestCompiler(false, false)]
        [TestCompiler(false, true)]
        public static int Bool22Ints(bool a, bool b)
        {
            return Vectors.ConvertToInt(new bool2(a, b));
        }

        // ---------------------------------------------------
        // double4
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static double Double4Int(int a)
        {
            return Vectors.ConvertToDouble(new double4(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Double3Double(double x)
        {
            return Vectors.ConvertToDouble(new double4(new double3(x), 5.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Double2Double2(double x)
        {
            return Vectors.ConvertToDouble(new double4(new double2(x), new double2(5.0f)));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double44Doubles(double a)
        {
            return Vectors.ConvertToDouble(new double4(1.0f, 2.0f, 3.0f + a, 4.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Double(double a)
        {
            return Vectors.ConvertToDouble(new double4(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Int4(ref int4 a)
        {
            return Vectors.ConvertToDouble((double4)new double4(a).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Half(double a)
        {
            var h = new half(a);
            return Vectors.ConvertToDouble((double4)new double4(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double4Half4(ref double4 a)
        {
            var h = new half4(a);
            return Vectors.ConvertToDouble((double4)new double4(h).x);
        }

        // ---------------------------------------------------
        // double3
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static double Double3Int(int a)
        {
            return Vectors.ConvertToDouble(new double3(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double33Doubles(double a)
        {
            return Vectors.ConvertToDouble(new double3(1.0f, 2.0f, 3.0f + a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double3Double(double a)
        {
            return Vectors.ConvertToDouble(new double3(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double3Double2Double(double a)
        {
            return Vectors.ConvertToDouble(new double3(new double2(a), 5.0f));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double3Half(double a)
        {
            var h = new half(a);
            return Vectors.ConvertToDouble((double3)new double3(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double3Half3(ref double3 a)
        {
            var h = new half3(a);
            return Vectors.ConvertToDouble((double3)new double3(h).x);
        }

        // ---------------------------------------------------
        // double2
        // ---------------------------------------------------

        [TestCompiler(DataRange.Standard)]
        public static double Double2Int(int a)
        {
            return Vectors.ConvertToDouble(new double2(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double22Doubles(double a)
        {
            return Vectors.ConvertToDouble(new double2(1.0f, 3.0f + a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double2Double(double a)
        {
            return Vectors.ConvertToDouble(new double2(a));
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double2Half(double a)
        {
            var h = new half(a);
            return Vectors.ConvertToDouble((double2)new double2(h).x);
        }

        [TestCompiler(DataRange.Standard)]
        public static double Double2Half2(ref double2 a)
        {
            var h = new half2(a);
            return Vectors.ConvertToDouble((double2)new double2(h).x);
        }


        [TestCompiler(uint.MaxValue, uint.MaxValue / 2 + 1)]
        public static float Float2UInt2(uint a, uint b)
        {
            return Vectors.ConvertToFloat(new float2(new uint2(a, b)));
        }

        [TestCompiler(uint.MaxValue, uint.MaxValue / 2 + 1)]
        public static float Float4UIntUIntUInt2Implicit(uint a, uint b)
        {
            var u = new uint2(a, b);
            return Vectors.ConvertToFloat(new float4(a, b, u));
        }

        [TestCompiler(uint.MaxValue, uint.MaxValue / 2 + 1)]
        public static float Float4UIntUIntUInt2Explicit(uint a, uint b)
        {
            var u = new uint2(a, b);
            return Vectors.ConvertToFloat(new float4(a, b, (float2) u));
        }
    }
}