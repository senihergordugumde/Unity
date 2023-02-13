using Unity.Collections;
using UnityEngine;

namespace PDNWrapper
{
    internal class Surface
    {
        NativeArray<Color32> m_Color;
        public Surface(int w, int h)
        {
            width = w;
            height = h;
            m_Color = new NativeArray<Color32>(width * height, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (m_Color.IsCreated)
            {
                m_Color.Dispose();
                m_Color = default;
            }
        }

        public NativeArray<Color32> color
        {
            get { return m_Color; }
        }

        public int width { get; private set; }
        public int height { get; private set; }
    }
}
