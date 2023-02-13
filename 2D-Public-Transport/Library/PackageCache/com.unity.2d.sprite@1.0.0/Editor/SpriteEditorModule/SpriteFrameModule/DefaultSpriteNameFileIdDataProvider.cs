using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal class DefaultSpriteNameFileIdDataProvider : ISpriteNameFileIdDataProvider
    {
        SpriteNameFileIdPair[] m_NameIDPair;

        public DefaultSpriteNameFileIdDataProvider(IEnumerable<SpriteRect> spriteRects)
        {
            m_NameIDPair = spriteRects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToArray();
        }

        public IEnumerable<SpriteNameFileIdPair> GetNameFileIdPairs()
        {
            return m_NameIDPair;
        }

        public void SetNameFileIdPairs(IEnumerable<SpriteNameFileIdPair> nameFileIdPairs)
        {
            m_NameIDPair = nameFileIdPairs.ToArray();
        }
    }
}
