using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ModuleHandle = UnityEngine.U2D.Common.UTess.ModuleHandle;

namespace UnityEditor.U2D.Animation
{
    internal class BoundedBiharmonicWeightsGenerator : IWeightsGenerator
    {
        internal static readonly BoneWeight defaultWeight = new BoneWeight() { weight0 = 1 };
        private const int kNumIterations = 100;
        private const int kNumSamples = 4;
        private const float kMinAngle = 20f;
        private const float kLargestTriangleAreaFactor = 0.4f;
        private const float kMeshAreaFactor = 0.004f;

        [DllImport("BoundedBiharmonicWeightsModule")]
        private static extern int Bbw(int iterations,
            [In, Out] IntPtr vertices, int vertexCount, int originalVertexCount,
            [In, Out] IntPtr indices, int indexCount,
            [In, Out] IntPtr controlPoints, int controlPointsCount,
            [In, Out] IntPtr boneEdges, int boneEdgesCount,    
            [In, Out] IntPtr pinIndices, int pinIndexCount,
            [In, Out] IntPtr weights
            );

        public BoneWeight[] Calculate(string name, Vector2[] vertices, int[] indices, Edge[] edges, Vector2[] controlPoints, Edge[] bones, int[] pins)
        {
            edges = SanitizeEdges(edges, vertices.Length);
            
            // In almost all cases subdivided mesh weights fine. Non-subdivide is only a fail-safe.
            bool success = false;
            var weights = CalculateInternal(name, vertices, indices, edges, controlPoints, bones, pins, kNumSamples, true, ref success);
            if (!success)
                weights = CalculateInternal(name, vertices, indices, edges, controlPoints, bones, pins, kNumSamples, false, ref success);
            return weights;
        }

        private Edge[] SanitizeEdges(Edge[] edges, int noOfVertices)
        {
            var tmpEdges = new List<Edge>(edges);
            for (var i = tmpEdges.Count - 1; i >= 0; i--)
            {
                if (tmpEdges[i].index1 >= noOfVertices || tmpEdges[i].index2 >= noOfVertices)
                    tmpEdges.RemoveAt(i);
            }

            return tmpEdges.ToArray();
        }
        
        // Round
        internal void Round(Vector2[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                float x = data[i].x;
                float y = data[i].y;
                x = (float) Math.Round(x, 8);
                y = (float) Math.Round(y, 8);
                data[i] = new Vector2(x, y);
            }        
        }

        internal BoneWeight[] CalculateInternal(string name, Vector2[] vertices, int[] indices, Edge[] edges, Vector2[] controlPoints, Edge[] bones, int[] pins, int numSamples, bool subdivide, ref bool done)
        {
            done = false;            
            var weights = new BoneWeight[vertices.Length];
            for (var i = 0; i < weights.Length; ++i)
                weights[i] = defaultWeight;
            if (vertices.Length < 3)
                return weights;            
            
            var boneSamples = SampleBones(controlPoints, bones, numSamples);
            var verticesList = new List<Vector2>(vertices.Length + controlPoints.Length + boneSamples.Length);
            Round(vertices);
            Round(controlPoints);
            Round(boneSamples);
            verticesList.AddRange(vertices);
            verticesList.AddRange(controlPoints);
            verticesList.AddRange(boneSamples);

            var utEdges = new List<Edge>(edges);
            var utIndices = new List<int>();
            var utVertices = new List<Vector2>(vertices);
            
            // Input Vertices are well refined and smoothed, just triangulate with bones and cages.
            bool ok = TriangulationUtility.TriangulateSafe(utVertices, utEdges, utIndices);
            if (!ok || utIndices.Count == 0)
            {
                utIndices.AddRange(indices);
                utVertices.AddRange(vertices);
            }
            else if(subdivide)
            {
                var targetArea = TriangulationUtility.FindTargetAreaForWeightMesh(utVertices, utIndices, kMeshAreaFactor, kLargestTriangleAreaFactor);
                TriangulationUtility.TessellateSafe(0, 0, 0, 0, targetArea, 1, 0, utVertices, utEdges, utIndices);
            }
            if (utIndices.Count == 0)
                return weights;

            // Copy Original Indices.
            var coIndices = new List<int>(utIndices);
            utIndices.Clear();
            for (int i = 0; i < coIndices.Count / 3; ++i)
            {
                int i1 = coIndices[0 + (i * 3)];
                int i2 = coIndices[1 + (i * 3)];
                int i3 = coIndices[2 + (i * 3)];
                float2 v1 = utVertices[i1];
                float2 v2 = utVertices[i2];
                float2 v3 = utVertices[i3];
                var rt = (float)Math.Round(ModuleHandle.OrientFast(v1, v2, v3), 2);
                if (rt != 0)
                {
                    utIndices.Add(i1);
                    utIndices.Add(i2);
                    utIndices.Add(i3);
                }
            }
            
            // Insert Samplers.
            var internalPoints = new List<int>();
            for (int i = 0; i < utVertices.Count; ++i)
                if (utIndices.Count(x => x == i) == 0)
                    internalPoints.Add(i);
            
            TriangulationUtility.TriangulateInternal(internalPoints.ToArray(), utVertices, utIndices);
            TriangulationUtility.TriangulateSamplers(boneSamples, utVertices, utIndices);
            TriangulationUtility.TriangulateSamplers(controlPoints, utVertices, utIndices);
            var tessellatedIndices = utIndices.ToArray();
            var tessellatedVertices = utVertices.ToArray();
            
            GCHandle verticesHandle = GCHandle.Alloc(tessellatedVertices, GCHandleType.Pinned);
            GCHandle indicesHandle = GCHandle.Alloc(tessellatedIndices, GCHandleType.Pinned);
            GCHandle controlPointsHandle = GCHandle.Alloc(controlPoints, GCHandleType.Pinned);
            GCHandle bonesHandle = GCHandle.Alloc(bones, GCHandleType.Pinned);
            GCHandle pinsHandle = GCHandle.Alloc(pins, GCHandleType.Pinned);
            GCHandle weightsHandle = GCHandle.Alloc(weights, GCHandleType.Pinned);

            int result = Bbw(kNumIterations,
                verticesHandle.AddrOfPinnedObject(), tessellatedVertices.Length, vertices.Length,
                indicesHandle.AddrOfPinnedObject(), tessellatedIndices.Length,
                controlPointsHandle.AddrOfPinnedObject(), controlPoints.Length,
                bonesHandle.AddrOfPinnedObject(), bones.Length,
                pinsHandle.AddrOfPinnedObject(), pins.Length,
                weightsHandle.AddrOfPinnedObject());
            
            switch (result)
            {
                case 1:
                case 2:
                    Debug.LogWarning($"Weight generation failure due to unexpected mesh input. Re-generate the mesh with a different Outline Detail value to resolve the issue. Error Code: {result}");
                    break;
                case 3:
                    if(!PlayerSettings.suppressCommonWarnings)
                        Debug.LogWarning($"Internal weight generation error. Error Code: {result}");
                    break;
            }

            verticesHandle.Free();
            indicesHandle.Free();
            controlPointsHandle.Free();
            bonesHandle.Free();
            pinsHandle.Free();
            weightsHandle.Free();
            
            // OhmDebugger.WriteStatisticsText(verticesList.ToArray(), edges, bones, boneSamples, weights, tessellatedVertices, tessellatedIndices, vertices.Length, controlPoints.Length, boneSamples.Length, "UTess", 1000);
            
            for (var i = 0; i < weights.Length; ++i)
            {
                var weight = weights[i];

                if (weight.Sum() == 0f)
                    weights[i] = defaultWeight;
                else if (!done)
                    done = (weight.boneIndex0 != 0 || weight.boneIndex1 != 0 || weight.boneIndex2 != 0 || weight.boneIndex3 != 0);
            }

            return weights;
        }

        public void DebugMesh(ISpriteMeshData spriteMeshData, Vector2[] vertices, Edge[] edges, Vector2[] controlPoints, Edge[] bones, int[] pins)
        {
            var boneSamples = SampleBones(controlPoints, bones, kNumSamples);
            var verticesList = new List<Vector2>(vertices.Length + controlPoints.Length + boneSamples.Length);
            var edgesList = new List<Edge>(edges);
            var indicesList = new List<int>();

            verticesList.AddRange(vertices);
            verticesList.AddRange(controlPoints);
            verticesList.AddRange(boneSamples);

            TriangulationUtility.Triangulate(verticesList, edgesList, indicesList, Allocator.Temp);

            spriteMeshData.Clear();

            verticesList.ForEach(v => spriteMeshData.AddVertex(v, new BoneWeight()));
            spriteMeshData.edges.AddRange(edgesList);
            spriteMeshData.indices.AddRange(indicesList);
        }

        private Vector2[] SampleBones(Vector2[] points, Edge[] edges, int numSamples)
        {
            Debug.Assert(numSamples > 0);

            var sampledEdges = new List<Vector2>();

            for (var i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                var tip = points[edge.index1];
                var tail = points[edge.index2];
                var length = (tip - tail).magnitude;

                for (var s = 0; s < numSamples; s++)
                {
                    var f = (s + 1f) / (float)(numSamples + 1f);
                    sampledEdges.Add(f * tail + (1f - f) * tip);
                }
            }

            return sampledEdges.ToArray();
        }
    }
}
