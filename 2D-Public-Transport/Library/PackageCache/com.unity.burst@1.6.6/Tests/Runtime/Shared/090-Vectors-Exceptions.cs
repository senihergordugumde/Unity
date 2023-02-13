using System;
using Unity.Burst;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    internal partial class VectorsExceptions
    {
        [TestCompiler(1.0f, ExpectedDiagnosticId = DiagnosticId.WRN_ExceptionThrownInNonSafetyCheckGuardedFunction)]
        public static float Float4WithException(float a)
        {
            return GetFloat4(a).x;
        }

        private static float4 GetFloat4(float value)
        {
            if (value < 0)
            {
                throw new ArgumentException();
                // Here the generated code should have a burst.abort + a return zero float4 (SIMD type)
            }
            return new float4(value);
        }
    }
}