using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.Burst.Editor
{
    internal class LongTextArea
    {
        private const int kMaxFragment = 2048;

        private struct Fragment
        {
            public int lineCount;
            public string text;
        }

        private string m_Text = "";
        private List<Fragment> m_Fragments = null;
        private Vector2[] m_AreaSizes = null;
        private bool invalidated = true;
        private Vector2 finalAreaSize;

        public string Text
        {
            get {
                return m_Text;
            }
            set {
                if (value != m_Text)
                {
                    m_Text = value;
                    m_Fragments = RecomputeFragments(m_Text);
                    invalidated = true;
                    m_AreaSizes = new Vector2[m_Fragments.Count];
                }
            }
        }

        // Changing the font size doesn't update the text field, so added this to force a recalculation
        public void Invalidate()
        {
            invalidated = true;
        }

        public void Render(GUIStyle style, Vector2 scrollPos, Rect workingArea)
        {
            // working area will be valid only during repaint, for the layout event we don't draw the labels
            style.richText = true;

            if (invalidated)
            {
                invalidated = false;
                int sizeIdx = 0;
                finalAreaSize = new Vector2(0.0f, 0.0f);
                foreach (var frag in m_Fragments)
                {
                    var size = style.CalcSize(new GUIContent(frag.text));
                    finalAreaSize.x = Math.Max(finalAreaSize.x, size.x);
                    finalAreaSize.y += size.y + style.padding.vertical;
                    m_AreaSizes[sizeIdx++] = size;
                }
            }

            GUILayoutUtility.GetRect(finalAreaSize.x,finalAreaSize.y);

            // NB we don't use workingArea or scrollPos, but if we find rendering is still too slow at a later date, we can use
            // these values to decide which chunks to render
            if (Event.current.type == EventType.Repaint)
            {
                float positionY = 0.0f;
                int sizeIdx = 0;
                foreach (var fragment in m_Fragments)
                {
                    var size = m_AreaSizes[sizeIdx++];
                    GUI.Label(new Rect(0.0f, positionY, finalAreaSize.x, size.y), fragment.text, style);
                    positionY += size.y + style.padding.vertical;
                }
            }
        }

        private static List<Fragment> RecomputeFragments(string text)
        {
            List<Fragment> result = new List<Fragment>();

            string[] pieces = text.Split('\n');

            StringBuilder b = new StringBuilder();
            int lineCount = 0;

            foreach (var piece in pieces)
            {
                if (b.Length >= kMaxFragment)
                {
                    AddFragment(b, lineCount, result);
                    lineCount = 0;
                }

                if (b.Length > 0)
                    b.Append('\n');

                b.Append(piece);
                lineCount++;
            }

            AddFragment(b, lineCount, result);

            return result;
        }

        private static void AddFragment(StringBuilder b, int lineCount, List<Fragment> result)
        {
            result.Add(new Fragment() { text = b.ToString(), lineCount = lineCount });
            b.Length = 0;
        }
    }

}