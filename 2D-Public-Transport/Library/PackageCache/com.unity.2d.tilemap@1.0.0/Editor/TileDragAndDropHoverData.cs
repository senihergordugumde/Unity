using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Tilemaps
{
    internal struct TileDragAndDropHoverData
    {
        public Object hoverObject;
        public Vector3 positionOffset;
        public Vector3 scaleFactor;
        public bool hasOffset;

        public TileDragAndDropHoverData(Object hoverObject) : this(hoverObject, Vector3.zero, Vector3.one, false)
        {
        }

        public TileDragAndDropHoverData(Object hoverObject, Vector3 positionOffset, Vector3 scaleFactor, bool hasOffset = true)
        {
            this.hoverObject = hoverObject;
            this.positionOffset = positionOffset;
            if (scaleFactor.z <= 0.0f)
                scaleFactor.z = 1.0f;
            this.scaleFactor = scaleFactor;
            this.hasOffset = hasOffset;
        }
    }
}
