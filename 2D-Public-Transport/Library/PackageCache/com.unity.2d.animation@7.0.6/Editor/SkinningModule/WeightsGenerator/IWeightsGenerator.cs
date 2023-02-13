using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal interface IWeightsGenerator
    {
        BoneWeight[] Calculate(string name, Vector2[] vertices, int[] indices, Edge[] edges, Vector2[] controlPoints, Edge[] bones, int[] pins);
    }
}
