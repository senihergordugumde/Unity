using System;
using UnityEngine;

namespace UnityEditor.U2D.Path.GUIFramework
{
    /// <summary>
    /// Represents a UI control in a custom editor.
    /// </summary>
    public abstract class Control
    {
        private string m_Name;
        private int m_NameHashCode;
        private int m_ID;
        private LayoutData m_LayoutData;
        private int m_ActionID = -1;
        private LayoutData m_HotLayoutData;

        /// <summary>
        /// The name of the control.
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// The control ID. The GUI uses this to identify the control.
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// The action ID.
        /// </summary>
        public int actionID
        {
            get { return m_ActionID; }
        }

        /// <summary>
        /// The control's layout data. This contains information about the control's position and orientation.
        /// </summary>
        public LayoutData layoutData
        {
            get { return m_LayoutData; }
            set { m_LayoutData = value; }
        }

        /// <summary>
        /// The control's hot layout data
        /// </summary>
        public LayoutData hotLayoutData
        {
            get { return m_HotLayoutData; }
        }

        /// <summary>
        /// Initializes and returns an instance of Control
        /// </summary>
        /// <param name="name">The name of the control</param>
        public Control(string name)
        {
            m_Name = name;
            m_NameHashCode = name.GetHashCode();
        }

        /// <summary>
        /// Gets the control from the guiState.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void GetControl(IGUIState guiState)
        {
            m_ID = guiState.GetControlID(m_NameHashCode, FocusType.Passive);
        }

        internal void SetActionID(int actionID)
        {
            m_ActionID = actionID;
            m_HotLayoutData = m_LayoutData;
        }

        /// <summary>
        /// Begins the layout for this control. A call to EndLayout must always follow a call to this function.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void BeginLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            m_LayoutData = OnBeginLayout(LayoutData.zero, guiState);
        }

        /// <summary>
        /// Gets the control's layout data from the guiState. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void Layout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            for (var i = 0; i < GetCount(); ++i)
            {
                if (guiState.hotControl == actionID && hotLayoutData.index == i)
                    continue;

                var layoutData = new LayoutData()
                {
                    index = i,
                    position = GetPosition(guiState, i),
                    distance = GetDistance(guiState, i),
                    forward = GetForward(guiState, i),
                    up = GetUp(guiState, i),
                    right = GetRight(guiState, i),
                    userData = GetUserData(guiState, i)
                };

                m_LayoutData = LayoutData.Nearest(m_LayoutData, layoutData);
            }
        }

        /// <summary>
        /// Ends the layout for this control. This function must always follow a call to BeginLayout().
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void EndLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            OnEndLayout(guiState);
        }

        /// <summary>
        /// Repaints the control. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void Repaint(IGUIState guiState)
        {
            for (var i = 0; i < GetCount(); ++i)
                OnRepaint(guiState, i);
        }

        /// <summary>
        /// Called when the control begins its layout.
        /// </summary>
        /// <param name="data">The layout data.</param>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns the layout data to use.</returns>
        protected virtual LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            return data;
        }

        /// <summary>
        /// Called when the control ends its layout.
        /// /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected virtual void OnEndLayout(IGUIState guiState)
        {
        }

        /// <summary>
        /// Called when the control repaints its contents.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        protected virtual void OnRepaint(IGUIState guiState, int index)
        {
        }

        /// <summary>
        /// Gets the number of sub-controllers.
        /// </summary>
        /// <remarks>
        /// By default, this is `1`. If you implement your own controller and want to use multiple sub-controllers within it, you can override this function to declare how to count the sub-controllers.
        /// </remarks>
        /// <returns>Returns the number of sub-controllers. If you do not override this function, this returns 1.</returns>
        protected virtual int GetCount()
        {
            return 1;
        }

        /// <summary>
        /// Gets the position of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.zero.</returns>
        protected virtual Vector3 GetPosition(IGUIState guiState, int index)
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the forward vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.forward.</returns>
        protected virtual Vector3 GetForward(IGUIState guiState, int index)
        {
            return Vector3.forward;
        }

        /// <summary>
        /// Gets the up vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.up,</returns>
        protected virtual Vector3 GetUp(IGUIState guiState, int index)
        {
            return Vector3.up;
        }

        /// <summary>
        /// Gets the right vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.right.</returns>
        protected virtual Vector3 GetRight(IGUIState guiState, int index)
        {
            return Vector3.right;
        }

        /// <summary>
        /// Gets the distance from the Scene view camera to the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns layoutData.distance.</returns>
        protected virtual float GetDistance(IGUIState guiState, int index)
        {
            return layoutData.distance;
        }

        /// <summary>
        /// Gets the control's user data. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns `null`.</returns>
        protected virtual object GetUserData(IGUIState guiState, int index)
        {
            return null;
        }
    }
}
