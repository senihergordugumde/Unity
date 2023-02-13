using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal class Triangulator : ITriangulator
    {
        public void Triangulate(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            TriangulationUtility.Triangulate(vertices, edges, indices, Allocator.Persistent);
        }

        public void Tessellate(float minAngle, float maxAngle, float meshAreaFactor, float largestTriangleAreaFactor, float areaThreshold, int smoothIterations, IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            TriangulationUtility.Tessellate(minAngle, maxAngle, meshAreaFactor, largestTriangleAreaFactor, areaThreshold, 10, smoothIterations, vertices, edges, indices, Allocator.Persistent);
        }
    }
}
