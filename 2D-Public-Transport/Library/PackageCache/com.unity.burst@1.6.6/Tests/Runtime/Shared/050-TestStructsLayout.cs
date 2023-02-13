// NOTE: Please read this before adding or changing anything in this file.
//
// This file doesn't contain any actual tests. It only contains structs.
// Tests are automatically generated from all structs in this file,
// which test:
// - the size of the struct
// - the offsets of each field
//
// When a struct contains a pointer, the test needs to use
// OverrideOn32BitNative so that wasm tests can compare with the correct
// values when testing 32-bit wasm on a 64-bit host platform.
// While it would be possible to use Roslyn to calculate these
// values automatically, for simplicity we use a couple of
// generator-specific attributes to set these manually:
// - [TestGeneratorOverride32BitSize(20)] should be set on a struct
// - [TestGeneratorOverride32BitOffset(12)] should be set on a field
// See the file below for examples.
//
// The test generation code lives in Burst.Compiler.IL.Tests.CodeGen.
// After making changes to this file, please run that project.
//
// The generated tests are in 050-TestStructsLayout.Generated.cs.

using System;
using System.Runtime.InteropServices;

namespace Burst.Compiler.IL.Tests
{
    partial class TestStructsLayout
    {
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private unsafe struct CheckHoleInner
        {
            [FieldOffset(0)]
            public byte* m_Ptr;
        }

        [TestGeneratorOverride32BitSize(20)]
        private struct CheckHoleOuter
        {
            public CheckHoleInner a;
            public int b;
            [TestGeneratorOverride32BitOffset(12)]
            public CheckHoleInner c;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ExplicitStructWithoutSize2
        {
            [FieldOffset(0)] public long a;
            [FieldOffset(8)] public sbyte b;
            [FieldOffset(9)] public int c;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ExplicitStructWithoutSize
        {
            [FieldOffset(0)] public int a;
            [FieldOffset(4)] public sbyte b;
            [FieldOffset(5)] public int c;
        }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        private struct SequentialStructWithSize3
        {
            public int a;
            public int b;
            public sbyte c;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SequentialStructWithoutSize
        {
            public int a;
            public int b;
            public sbyte c;
        }

        private struct SequentialStructEmptyNoAttributes { }

        [StructLayout(LayoutKind.Explicit)]
        private struct ExplicitStructWithEmptySequentialFields
        {
            [FieldOffset(0)] public SequentialStructEmptyNoAttributes FieldA;
            [FieldOffset(0)] public SequentialStructEmptyNoAttributes FieldB;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ExplicitStrictWithEmptyAndNonEmptySequentialFields
        {
            [FieldOffset(0)] public SequentialStructEmptyNoAttributes FieldA;
            [FieldOffset(0)] public SequentialStructWithoutSize FieldB;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct StructWithPack8
        {
            public int FieldA;
            public int FieldB;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct StructPack2WithBytesAndInt
        {
            public byte FieldA;
            public byte FieldB;
            public int FieldC;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct StructPack2WithBytesAndInts
        {
            public byte FieldA;
            public byte FieldB;
            public int FieldC;
            public int FieldD;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct StructPack1WithBytesAndInt
        {
            public byte FieldA;
            public byte FieldB;
            public int FieldC;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct StructPack1WithByteAndInt
        {
            public byte FieldA;
            public int FieldB;
        }

        private struct StructPack1WithByteAndIntWrapper
        {
            public StructPack1WithByteAndInt FieldA;
            public StructPack1WithByteAndInt FieldB;
        }

        private struct StructPack1WithByteAndIntWrapper2
        {
            public StructPack1WithByteAndIntWrapper FieldA;
            public StructPack1WithByteAndIntWrapper FieldB;
        }

        [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)]
        private struct StructWithSizeAndPack
        {
            public double FieldA;
            public int FieldB;
        }

        private struct StructWithSizeAndPackWrapper
        {
            public byte FieldA;
            public StructWithSizeAndPack FieldB;
        }

        [StructLayout(LayoutKind.Explicit, Size = 12, Pack = 4)]
        private struct StructWithSizeAndPack4
        {
            [FieldOffset(0)]
            public double FieldA;
            [FieldOffset(8)]
            public int FieldB;
        }

        private struct StructWithSizeAndPack4Wrapper
        {
            public byte FieldA;
            public StructWithSizeAndPack4 FieldB;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct StructExplicitPack1WithByteAndInt
        {
            [FieldOffset(0)]
            public byte FieldA;

            [FieldOffset(1)]
            public int FieldB;
        }

        private struct StructExplicitPack1WithByteAndIntWrapper
        {
            public StructExplicitPack1WithByteAndInt FieldA;
            public StructExplicitPack1WithByteAndInt FieldB;
        }

        private struct StructExplicitPack1WithByteAndIntWrapper2
        {
            public StructExplicitPack1WithByteAndIntWrapper FieldA;
            public StructExplicitPack1WithByteAndIntWrapper FieldB;
        }

        [StructLayout(LayoutKind.Explicit, Size = 12, Pack = 1)]
        private struct StructExplicitWithSizeAndPack
        {
            [FieldOffset(0)]
            public double FieldA;
            [FieldOffset(8)]
            public int FieldB;
        }

        private struct StructExplicitWithSizeAndPackWrapper
        {
            public byte FieldA;
            public StructExplicitWithSizeAndPack FieldB;
        }

        [StructLayout(LayoutKind.Explicit, Size = 12, Pack = 4)]
        private struct StructExplicitWithSizeAndPack4
        {
            [FieldOffset(0)]
            public double FieldA;
            [FieldOffset(8)]
            public int FieldB;
        }

        private struct StructExplicitWithSizeAndPack4Wrapper
        {
            public byte FieldA;
            public StructExplicitWithSizeAndPack4 FieldB;
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    internal sealed class TestGeneratorOverride32BitSizeAttribute : Attribute
    {
        public readonly int Size;

        public TestGeneratorOverride32BitSizeAttribute(int size)
        {
            Size = size;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class TestGeneratorOverride32BitOffsetAttribute : Attribute
    {
        public readonly int Offset;

        public TestGeneratorOverride32BitOffsetAttribute(int offset)
        {
            Offset = offset;
        }
    }
}
