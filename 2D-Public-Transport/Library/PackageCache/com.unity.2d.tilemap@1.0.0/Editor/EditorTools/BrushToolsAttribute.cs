using System;
using System.Collections.Generic;

namespace UnityEditor.Tilemaps
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BrushToolsAttribute : Attribute
    {
        private List<Type> m_ToolTypes;
        internal List<Type> toolList
        {
            get { return m_ToolTypes; }
        }

        public BrushToolsAttribute(params Type[] tools)
        {
            m_ToolTypes = new List<Type>();
            foreach (var toolType in tools)
            {
                if (toolType.IsSubclassOf(typeof(TilemapEditorTool)) && !m_ToolTypes.Contains(toolType))
                {
                    m_ToolTypes.Add(toolType);
                }
            }
        }
    }
}
