using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.U2D.Animation
{
    internal class SpriteSelectorWidget
    {
        class Styles
        {
            public GUIStyle gridListStyle;

            public Styles()
            {
                gridListStyle = new GUIStyle("GridList");
                gridListStyle.alignment = GUI.skin.button.alignment;
            }
        }

        Sprite[] m_SpriteList = new Sprite[0];
        Texture2D[] m_SpritePreviews = new Texture2D[0];
        List<int> m_SpritePreviewNeedFetching = new List<int>();
        Vector2 m_ScrollPos;
        Styles m_Style;
        const int kTargetPreviewSize = 64;
        public SpriteSelectorWidget()
        {}

        public void UpdateContents(Sprite[] sprites)
        {
            m_SpriteList = sprites;
            m_SpritePreviews = new Texture2D[sprites.Length];
            for (int i = 0; i < m_SpritePreviews.Length; ++i)
                m_SpritePreviewNeedFetching.Add(i);
            UpdateSpritePreviews();
        }

        
        public int ShowGUI(int selectedIndex)
        {
            if (m_Style == null)
                m_Style = new Styles();

            UpdateSpritePreviews();
            
            var drawRect = EditorGUILayout.GetControlRect(false, kTargetPreviewSize + 10f, new[] {GUILayout.ExpandWidth(true)});
            if(Event.current.type == EventType.Repaint)
                GUI.skin.box.Draw(drawRect, false, false, false, false);
            if (m_SpriteList == null || m_SpriteList.Length == 0)
            {
                return selectedIndex;
            }
            selectedIndex = (selectedIndex > m_SpriteList.Length) ? 0 : selectedIndex;
            
            float columnF;
            int columnCount, rowCount;
            GetRowColumnCount(drawRect.width - 20, kTargetPreviewSize, m_SpriteList.Length, out columnCount, out rowCount, out columnF);
            if (columnCount > 0 && rowCount > 0)
            {
                float contentSize = (columnF * kTargetPreviewSize) / columnCount;
                var gridRect = new Rect(drawRect.x, drawRect.y, drawRect.width - 20, rowCount * contentSize);
                m_ScrollPos = GUI.BeginScrollView(drawRect,m_ScrollPos, gridRect, false, false,  GUIStyle.none, GUI.skin.verticalScrollbar);
            
                m_Style.gridListStyle.fixedWidth = contentSize;
                m_Style.gridListStyle.fixedHeight = contentSize;
                selectedIndex = GUI.SelectionGrid( gridRect, selectedIndex, m_SpritePreviews, columnCount, m_Style.gridListStyle);
                GUI.EndScrollView();
            }
            return selectedIndex;
        }

        static void GetRowColumnCount(float drawWidth, int size, int contentCount, out int column, out int row, out float columnf)
        {
            columnf = drawWidth / size;
            column = (int)Mathf.Floor(columnf);
            if (column == 0)
                row = 0;
            else
                row = (int)Mathf.Ceil((contentCount + column) / column);
        }

        public bool NeedUpdatePreview()
        {
            return m_SpritePreviewNeedFetching.Count > 0;
        }
        
        void UpdateSpritePreviews()
        {
            for (int i = 0; i < m_SpritePreviewNeedFetching.Count; ++i)
            {
                var index = m_SpritePreviewNeedFetching[i];
                if(m_SpriteList[index] == null)
                    m_SpritePreviews[index] = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
                else
                    m_SpritePreviews[index] = AssetPreview.GetAssetPreview(m_SpriteList[index]);
                if (m_SpritePreviews[index] != null)
                {
                    m_SpritePreviewNeedFetching.RemoveAt(i);
                    --i;
                }
            }
        }
    }
}
