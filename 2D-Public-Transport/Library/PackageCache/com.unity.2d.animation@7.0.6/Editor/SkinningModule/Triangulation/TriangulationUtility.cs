using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using ModuleHandle = UnityEngine.U2D.Common.UTess.ModuleHandle;
#if ENABLE_ANIMATION_BURST
using Unity.Burst;
#endif

namespace UnityEditor.U2D.Animation
{
    [BurstCompile]
    internal class TriangulationUtility
    {
        
        // Adjust Tolerance for Collinear Check.
        static readonly float k_CollinearTolerance = 0.0001f;
        
#if ENABLE_ANIMATION_BURST
        [BurstCompile]
#endif
        private static unsafe int ValidateCollinear(float2* points, int pointCount, float epsilon)
        {
            if (pointCount < 3)
                return 0;
            
            for (int i = 0; i < pointCount - 2; ++i)
            {
                double2 a = points[i];
                double2 b = points[i + 1];
                double2 c = points[i + 2];               
                
                // Just check area of triangle and see if its non-zero. 
                var x = math.abs(a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
                if (x > epsilon)
                    return 1;
            }

            return 0;
        }
        
#if ENABLE_ANIMATION_BURST
        [BurstCompile]
#endif
        private static unsafe void TessellateBurst(Allocator allocator, float2* points, int pointCount, int2* edges, int edgeCount, float2* outVertices, int* outIndices, int2* outEdges, int arrayCount, int3* result)
        {

            NativeArray<int2> _edges = new NativeArray<int2>(edgeCount, allocator);
            for (int i = 0; i < _edges.Length; ++i)
                _edges[i] = edges[i];
            
            NativeArray<float2> _points = new NativeArray<float2>(pointCount, allocator);
            for (int i = 0; i < _points.Length; ++i)
                _points[i] = points[i];
            
            NativeArray<int> _outIndices = new NativeArray<int>(arrayCount, allocator);
            NativeArray<int2> _outEdges = new NativeArray<int2>(arrayCount, allocator);
            NativeArray<float2> _outVertices = new NativeArray<float2>(arrayCount, allocator);
            
            int outEdgeCount = 0;
            int outIndexCount = 0;
            int outVertexCount = 0;
            
            var check = ValidateCollinear((float2*)_points.GetUnsafeReadOnlyPtr(), pointCount, k_CollinearTolerance);
            if (0 != check)
                ModuleHandle.Tessellate(allocator, _points, _edges, ref _outVertices, ref outVertexCount, ref _outIndices, ref outIndexCount, ref _outEdges, ref outEdgeCount);
            
            for (int i = 0; i < outEdgeCount; ++i)
                outEdges[i] = _outEdges[i];
            for (int i = 0; i < outIndexCount; ++i)
                outIndices[i] = _outIndices[i];
            for (int i = 0; i < outVertexCount; ++i)
                outVertices[i] = _outVertices[i];
            
            result->x = outVertexCount;
            result->y = outIndexCount;
            result->z = outEdgeCount;

            _outVertices.Dispose();
            _outEdges.Dispose();
            _outIndices.Dispose();
            _points.Dispose();
            _edges.Dispose();

        }
#if ENABLE_ANIMATION_BURST
        [BurstCompile]
#endif
        private static unsafe void SubdivideBurst(Allocator allocator, float2* points, int pointCount, int2* edges, int edgeCount, float2* outVertices, int* outIndices, int2* outEdges, int arrayCount, float areaFactor, float areaThreshold, int refineIterations, int smoothenIterations, int3* result)
        {
            NativeArray<int2> _edges = new NativeArray<int2>(edgeCount, allocator);
            for (int i = 0; i < _edges.Length; ++i)
                _edges[i] = edges[i];
            
            NativeArray<float2> _points = new NativeArray<float2>(pointCount, allocator);
            for (int i = 0; i < _points.Length; ++i)
                _points[i] = points[i];
            
            NativeArray<int> _outIndices = new NativeArray<int>(arrayCount, allocator);
            NativeArray<int2> _outEdges = new NativeArray<int2>(arrayCount, allocator);
            NativeArray<float2> _outVertices = new NativeArray<float2>(arrayCount, allocator);
            int outEdgeCount = 0;
            int outIndexCount = 0;
            int outVertexCount = 0;            
            
            ModuleHandle.Subdivide(allocator, _points, _edges, ref _outVertices, ref outVertexCount, ref _outIndices, ref outIndexCount, ref _outEdges, ref outEdgeCount, areaFactor, areaThreshold, refineIterations, smoothenIterations);
            
            for (int i = 0; i < outEdgeCount; ++i)
                outEdges[i] = _outEdges[i];
            for (int i = 0; i < outIndexCount; ++i)
                outIndices[i] = _outIndices[i];
            for (int i = 0; i < outVertexCount; ++i)
                outVertices[i] = _outVertices[i];   
            
            result->x = outVertexCount;
            result->y = outIndexCount;
            result->z = outEdgeCount;
            
            _outVertices.Dispose();
            _outEdges.Dispose();
            _outIndices.Dispose();
            _points.Dispose();
            _edges.Dispose();            
        }

        private static bool TessellateSafe(NativeArray<float2> points, NativeArray<int2> edges, ref NativeArray<float2> outVertices, ref int outVertexCount, ref NativeArray<int> outIndices, ref int outIndexCount, ref NativeArray<int2> outEdges, ref int outEdgeCount)
        {
            unsafe
            {
                var check = ValidateCollinear((float2*)points.GetUnsafeReadOnlyPtr(), points.Length, k_CollinearTolerance);
                if (0 == check)
                    return false;                
            }
            
            try
            {
                ModuleHandle.Tessellate(Allocator.Persistent, points, edges, ref outVertices, ref outVertexCount, ref outIndices, ref outIndexCount, ref outEdges, ref outEdgeCount);
            }
            catch (Exception)
            {
                return false;
            }
            return true;            
        }
        private static bool SubdivideSafe(NativeArray<float2> points, NativeArray<int2> edges, ref NativeArray<float2> outVertices, ref int outVertexCount, ref NativeArray<int> outIndices, ref int outIndexCount, ref NativeArray<int2> outEdges, ref int outEdgeCount, float areaFactor, float areaThreshold, int refineIterations, int smoothenIterations)
        {
            try
            {
                ModuleHandle.Subdivide(Allocator.Persistent, points, edges, ref outVertices, ref outVertexCount, ref outIndices, ref outIndexCount, ref outEdges, ref outEdgeCount, areaFactor, areaThreshold, refineIterations, smoothenIterations);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        internal static void Quad(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices, Allocator allocator)
        {
            if (vertices.Count < 3)
                return;

            NativeArray<float2> points = new NativeArray<float2>(vertices.Count, allocator);
            for (int i = 0; i < vertices.Count; ++i)
                points[i] = vertices[i];

            int arrayCount = vertices.Count * vertices.Count * 4;
            int vertexCount = 0, indexCount = 0, edgeCount = 0;
            NativeArray<int> outputIndices = new NativeArray<int>(arrayCount, allocator);
            NativeArray<int2> outputEdges = new NativeArray<int2>(arrayCount, allocator);
            NativeArray<float2> outputVertices = new NativeArray<float2>(arrayCount, allocator);
            
            NativeArray<int2> fallback = new NativeArray<int2>(0, allocator);
            TessellateSafe(points, fallback, ref outputVertices, ref vertexCount, ref outputIndices,
                ref indexCount, ref outputEdges, ref edgeCount);
            fallback.Dispose();

            vertices.Clear();
            for (int i = 0; i < vertexCount; ++i)
                vertices.Add(outputVertices[i]);
            indices.Clear();
            for (int i = 0; i < indexCount; ++i)
                indices.Add(outputIndices[i]);
            edges.Clear();
            for (int i = 0; i < edgeCount; ++i)
                edges.Add(new Edge(){index1 = outputEdges[i].x, index2 = outputEdges[i].y});

            outputEdges.Dispose();
            outputIndices.Dispose();
            outputVertices.Dispose();
            points.Dispose();            
        }
        
        internal static void Triangulate(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices, Allocator allocator)
        {
            if (vertices.Count < 3)
                return;

            NativeArray<float2> points = new NativeArray<float2>(vertices.Count, allocator);
            for (int i = 0; i < vertices.Count; ++i)
                points[i] = vertices[i];
            NativeArray<int2> inputEdges = new NativeArray<int2>(edges.Count, allocator);
            for (int i = 0; i < edges.Count; ++i)
                inputEdges[i] = new int2(edges[i].index1, edges[i].index2);
            
            int arrayCount = vertices.Count * vertices.Count * 4;
            int vertexCount = 0, indexCount = 0, edgeCount = 0;
            NativeArray<int> outputIndices = new NativeArray<int>(arrayCount, allocator);
            NativeArray<int2> outputEdges = new NativeArray<int2>(arrayCount, allocator);
            NativeArray<int3> outputResult = new NativeArray<int3>(1, allocator);
            NativeArray<float2> outputVertices = new NativeArray<float2>(arrayCount, allocator);
            
            unsafe
            {
                TessellateBurst(allocator, (float2*)points.GetUnsafePtr(), points.Length, (int2*)inputEdges.GetUnsafePtr(), inputEdges.Length, (float2*)outputVertices.GetUnsafePtr(), (int*)outputIndices.GetUnsafePtr(), (int2*)outputEdges.GetUnsafePtr(), arrayCount, (int3*)outputResult.GetUnsafePtr());
                vertexCount = outputResult[0].x;
                indexCount = outputResult[0].y;
                edgeCount = outputResult[0].z;
            }
            // Fallback on numerical precision errors.
            if (vertexCount <= 8 || indexCount == 0)
                TessellateSafe(points, inputEdges, ref outputVertices, ref vertexCount, ref outputIndices, ref indexCount, ref outputEdges, ref edgeCount);

            vertices.Clear();
            for (int i = 0; i < vertexCount; ++i)
                vertices.Add(outputVertices[i]);
            indices.Clear();
            for (int i = 0; i < indexCount; ++i)
                indices.Add(outputIndices[i]);
            edges.Clear();
            for (int i = 0; i < edgeCount; ++i)
                edges.Add(new Edge(){index1 = outputEdges[i].x, index2 = outputEdges[i].y});

            outputEdges.Dispose();
            outputResult.Dispose();
            outputIndices.Dispose();
            outputVertices.Dispose();
            inputEdges.Dispose();
            points.Dispose();
        }
        
        internal static bool TriangulateSafe(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            if (vertices.Count < 3)
                return false;

            NativeArray<float2> points = new NativeArray<float2>(vertices.Count, Allocator.Persistent);
            for (int i = 0; i < vertices.Count; ++i)
                points[i] = vertices[i];
            NativeArray<int2> inputEdges = new NativeArray<int2>(edges.Count, Allocator.Persistent);
            for (int i = 0; i < edges.Count; ++i)
                inputEdges[i] = new int2(edges[i].index1, edges[i].index2);
            
            int arrayCount = vertices.Count * vertices.Count * 4;
            int vertexCount = 0, indexCount = 0, edgeCount = 0;
            NativeArray<int> outputIndices = new NativeArray<int>(arrayCount, Allocator.Persistent);
            NativeArray<int2> outputEdges = new NativeArray<int2>(arrayCount, Allocator.Persistent);
            NativeArray<float2> outputVertices = new NativeArray<float2>(arrayCount, Allocator.Persistent);
            bool ok = TessellateSafe(points, inputEdges, ref outputVertices, ref vertexCount, ref outputIndices, ref indexCount, ref outputEdges, ref edgeCount);

            if (ok)
            {
                vertices.Clear();
                for (int i = 0; i < vertexCount; ++i)
                    vertices.Add(outputVertices[i]);
                indices.Clear();
                for (int i = 0; i < indexCount; ++i)
                    indices.Add(outputIndices[i]);
                edges.Clear();
                for (int i = 0; i < edgeCount; ++i)
                    edges.Add(new Edge() {index1 = outputEdges[i].x, index2 = outputEdges[i].y});
            }

            outputEdges.Dispose();
            outputIndices.Dispose();
            outputVertices.Dispose();
            inputEdges.Dispose();
            points.Dispose();
            return ok;
        }        

        public static void Tessellate(float minAngle, float maxAngle, float meshAreaFactor, float largestTriangleAreaFactor, float targetArea, int refineIterations, int smoothenIterations, IList<Vector2> vertices, IList<Edge> edges, IList<int> indices, Allocator allocator)
        {
            if (vertices.Count < 3)
                return;

            largestTriangleAreaFactor = Mathf.Clamp01(largestTriangleAreaFactor);

            NativeArray<float2> points = new NativeArray<float2>(vertices.Count, allocator);
            for (int i = 0; i < vertices.Count; ++i)
                points[i] = vertices[i];
            NativeArray<int2> inputEdges = new NativeArray<int2>(edges.Count, allocator);
            for (int i = 0; i < edges.Count; ++i)
                inputEdges[i] = new int2(edges[i].index1, edges[i].index2);
            
            int maxDataCount = 65536;
            int vertexCount = 0, indexCount = 0, edgeCount = 0;
            NativeArray<int> outputIndices = new NativeArray<int>(maxDataCount, allocator);
            NativeArray<int2> outputEdges = new NativeArray<int2>(maxDataCount, allocator);
            NativeArray<int3> outputResult = new NativeArray<int3>(1, allocator);
            NativeArray<float2> outputVertices = new NativeArray<float2>(maxDataCount, allocator);
            
            unsafe
            {
                SubdivideBurst(allocator, (float2*)points.GetUnsafePtr(), points.Length, (int2*)inputEdges.GetUnsafePtr(), inputEdges.Length, (float2*)outputVertices.GetUnsafePtr(), (int*)outputIndices.GetUnsafePtr(), (int2*)outputEdges.GetUnsafePtr(), maxDataCount, largestTriangleAreaFactor, targetArea, refineIterations, smoothenIterations, (int3*)outputResult.GetUnsafePtr());
                vertexCount = outputResult[0].x;
                indexCount = outputResult[0].y;
                edgeCount = outputResult[0].z;                
            }
            // Fallback on numerical precision errors.
            if (vertexCount <= 8)
                SubdivideSafe(points, inputEdges, ref outputVertices, ref vertexCount, ref outputIndices, ref indexCount, ref outputEdges, ref edgeCount, largestTriangleAreaFactor, targetArea, refineIterations, smoothenIterations);

            vertices.Clear();
            for (int i = 0; i < vertexCount; ++i)
                vertices.Add(outputVertices[i]);
            indices.Clear();
            for (int i = 0; i < indexCount; ++i)
                indices.Add(outputIndices[i]);
            edges.Clear();
            for (int i = 0; i < edgeCount; ++i)
                edges.Add(new Edge(){index1 = outputEdges[i].x, index2 = outputEdges[i].y});

            outputEdges.Dispose();
            outputResult.Dispose();
            outputIndices.Dispose();
            outputVertices.Dispose();
            inputEdges.Dispose();
            points.Dispose();
        }

        public static void TessellateSafe(float minAngle, float maxAngle, float meshAreaFactor, float largestTriangleAreaFactor, float targetArea, int refineIterations, int smoothenIterations, IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            if (vertices.Count < 3)
                return;

            largestTriangleAreaFactor = Mathf.Clamp01(largestTriangleAreaFactor);

            NativeArray<float2> points = new NativeArray<float2>(vertices.Count, Allocator.Persistent);
            for (int i = 0; i < vertices.Count; ++i)
                points[i] = vertices[i];
            NativeArray<int2> inputEdges = new NativeArray<int2>(edges.Count, Allocator.Persistent);
            for (int i = 0; i < edges.Count; ++i)
                inputEdges[i] = new int2(edges[i].index1, edges[i].index2);
            
            int vertexCount = 0, indexCount = 0, edgeCount = 0, maxDataCount = 65536;
            NativeArray<float2> outputVertices = new NativeArray<float2>(maxDataCount, Allocator.Persistent);
            NativeArray<int> outputIndices = new NativeArray<int>(maxDataCount, Allocator.Persistent);
            NativeArray<int2> outputEdges = new NativeArray<int2>(maxDataCount, Allocator.Persistent);
            bool ok = SubdivideSafe(points, inputEdges, ref outputVertices, ref vertexCount, ref outputIndices, ref indexCount, ref outputEdges, ref edgeCount, largestTriangleAreaFactor, targetArea, refineIterations, smoothenIterations) ;

            if (ok)
            {
                vertices.Clear();
                for (int i = 0; i < vertexCount; ++i)
                    vertices.Add(outputVertices[i]);
                indices.Clear();
                for (int i = 0; i < indexCount; ++i)
                    indices.Add(outputIndices[i]);
                edges.Clear();
                for (int i = 0; i < edgeCount; ++i)
                    edges.Add(new Edge() {index1 = outputEdges[i].x, index2 = outputEdges[i].y});
            }

            outputEdges.Dispose();
            outputIndices.Dispose();
            outputVertices.Dispose();
            inputEdges.Dispose();
            points.Dispose();
        }        
        
        // Find Target Area to Subdivide for BBW. todo: Burst it.
        internal static float FindTargetAreaForWeightMesh(List<Vector2> triVertices, List<int> triIndices, float meshAreaFactor, float largestTriangleFactor)
        {
            float totalArea = 0, largestArea = 0, targetArea = 0;
            
            for (int i = 0; i < triIndices.Count / 3; ++i)
            {
                int i1 = triIndices[0 + (i * 3)];
                int i2 = triIndices[1 + (i * 3)];
                int i3 = triIndices[2 + (i * 3)];
                float2 v1 = triVertices[i1];
                float2 v2 = triVertices[i2];
                float2 v3 = triVertices[i3];
                float area = ModuleHandle.TriangleArea(v1, v2, v3);
                totalArea = totalArea + area;
                largestArea = largestArea > area ? largestArea : area;
            }

            targetArea = Mathf.Max(meshAreaFactor * totalArea, largestTriangleFactor * largestArea);
            return targetArea;
        }        
        
        // Triangulate Bone Samplers.  todo: Burst it.
        internal static void TriangulateSamplers(Vector2[] samplers, List<Vector2> triVertices, List<int> triIndices)
        {
            foreach(var v in samplers)
            {
                var vertexCount = triVertices.Count;
                
                for (int i = 0; i < triIndices.Count / 3; ++i)
                {
                    int i1 = triIndices[0 + (i * 3)];
                    int i2 = triIndices[1 + (i * 3)];
                    int i3 = triIndices[2 + (i * 3)];
                    float2 v1 = triVertices[i1];
                    float2 v2 = triVertices[i2];
                    float2 v3 = triVertices[i3];
                    var inside = ModuleHandle.IsInsideTriangle(v, v1, v2, v3);
                    if (inside)
                    {
                        triVertices.Add(v);
                        triIndices.Add(i1); triIndices.Add(i2); triIndices.Add(vertexCount);
                        triIndices.Add(i2); triIndices.Add(i3); triIndices.Add(vertexCount);
                        triIndices.Add(i3); triIndices.Add(i1); triIndices.Add(vertexCount);
                        break;
                    }
                }
            }
        }
        
        
        // Triangulate Skipped Original Points. These points are discarded during PlanarGrapg cleanup. But bbw only cares if these are part of any geometry. So just insert them. todo: Burst it. 
        internal static void TriangulateInternal(int[] internalIndices, List<Vector2> triVertices, List<int> triIndices)
        {
            var triangleCount = triIndices.Count / 3;
            
            foreach(var index in internalIndices)
            {
                var v = triVertices[index];
                for (int i = 0; i < triangleCount; ++i)
                {
                    int i1 = triIndices[0 + (i * 3)];
                    int i2 = triIndices[1 + (i * 3)];
                    int i3 = triIndices[2 + (i * 3)];
                    float2 v1 = triVertices[i1];
                    float2 v2 = triVertices[i2];
                    float2 v3 = triVertices[i3];
                    var c1 = (float)Math.Round(ModuleHandle.OrientFast(v1, v2, v), 2);
                    if (c1 == 0)
                    {
                        triIndices[0 + (i * 3)] = i1; triIndices[1 + (i * 3)] = index; triIndices[2 + (i * 3)] = i3;
                        triIndices.Add(index); triIndices.Add(i2); triIndices.Add(i3);
                    }
                    else
                    {
                        var c2 = (float)Math.Round(ModuleHandle.OrientFast(v2, v3, v), 2);
                        if (c2 == 0)
                        {
                            triIndices[0 + (i * 3)] = i2; triIndices[1 + (i * 3)] = index; triIndices[2 + (i * 3)] = i1;
                            triIndices.Add(index); triIndices.Add(i3); triIndices.Add(i1);
                        }
                        else
                        {
                            var c3 = (float)Math.Round(ModuleHandle.OrientFast(v3, v1, v), 2);
                            if (c3 == 0)
                            {
                                triIndices[0 + (i * 3)] = i3; triIndices[1 + (i * 3)] = index; triIndices[2 + (i * 3)] = i2;
                                triIndices.Add(index); triIndices.Add(i1); triIndices.Add(i2);
                            }
                        }
                    }                    
                }
            }
        }
        
    }
}
