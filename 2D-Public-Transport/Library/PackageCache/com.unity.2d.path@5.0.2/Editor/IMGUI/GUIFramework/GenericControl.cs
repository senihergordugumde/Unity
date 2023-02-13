using System;
using UnityEngine;

namespace UnityEditor.U2D.Path.GUIFramework
{
    /// <summary>
    /// Represents a generic UI control.
    /// </summary>
    public class GenericControl : Control
    {
        /// <summary>
        /// Func for OnBeginLayout
        /// </summary>
        public Func<IGUIState, LayoutData> onBeginLayout;
        /// <summary>
        /// Action for OnEndLayout
        /// </summary>
        public Action<IGUIState> onEndLayout;
        /// <summary>
        /// Action for OnRepaint
        /// </summary>
        public Action<IGUIState, Control, int> onRepaint;
        /// <summary>
        /// Func for GetCount
        /// </summary>
        public Func<int> count;
        /// <summary>
        /// Func for GetPosition
        /// </summary>
        public Func<int, Vector3> position;
        /// <summary>
        /// Func for GetDistance
        /// </summary>
        public Func<IGUIState, int, float> distance;
        /// <summary>
        /// Func for GetForward
        /// </summary>
        public Func<int, Vector3> forward;
        /// <summary>
        /// Func for GetUp
        /// </summary>
        public Func<int, Vector3> up;
        /// <summary>
        /// Func for GetRight
        /// </summary>
        public Func<int, Vector3> right;
        /// <summary>
        /// Func for GetUserData
        /// </summary>
        public Func<int, object> userData;

        /// <summary>
        /// Initializes and returns an instance of GenericControl
        /// </summary>
        /// <param name="name">The name of the generic control.</param>
        public GenericControl(string name) : base(name)
        {
        }


        /// <summary>
        /// Gets the number of sub-controllers.
        /// </summary>
        /// <remarks>
        /// By default, this is `1`. If you implement your own controller and want to use multiple sub-controllers within it, you can assign getCount to a function that returns the number of sub-controllers.
        /// </remarks>
        /// <returns>Returns the number of sub-controllers. If you do not assign getCount, this returns 1.</returns>
        protected override int GetCount()
        {
            if (count != null)
                return count();

            return base.GetCount();
        }

        /// <summary>
        /// Called when the control ends its layout.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected override void OnEndLayout(IGUIState guiState)
        {
            if (onEndLayout != null)
                onEndLayout(guiState);
        }

        /// <summary>
        /// Called when the control repaints its contents.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">Current Index</param>
        protected override void OnRepaint(IGUIState guiState, int index)
        {
            if (onRepaint != null)
                onRepaint(guiState, this, index);
        }

        /// <summary>
        /// Called when the control begins its layout.
        /// </summary>
        /// <param name="data">The layout data.</param>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>The LayoutData</returns>
        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            if (onBeginLayout != null)
                return onBeginLayout(guiState);

            return data;
        }

        /// <summary>
        /// Gets the position of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>The position</returns>
        protected override Vector3 GetPosition(IGUIState guiState, int index)
        {
            if (position != null)
                return position(index);

            return base.GetPosition(guiState,index);
        }

        /// <summary>
        /// Gets the distance from the Scene view camera to the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Returns the distance from the Scene view camera to the control.</returns>
        protected override float GetDistance(IGUIState guiState, int index)
        {
            if (distance != null)
                return distance(guiState, index);

            return base.GetDistance(guiState, index);
        }

        /// <summary>
        /// Gets the forward vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Returns the generic control's forward vector.</returns>
        protected override Vector3 GetForward(IGUIState guiState, int index)
        {
            if (forward != null)
                return forward(index);

            return base.GetForward(guiState,index);
        }

        /// <summary>
        /// Gets the up vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Returns the generic control's up vector.</returns>
        protected override Vector3 GetUp(IGUIState guiState, int index)
        {
            if (up != null)
                return up(index);

            return base.GetUp(guiState,index);
        }

        /// <summary>
        /// Gets the right vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Returns the generic control's right vector.</returns>
        protected override Vector3 GetRight(IGUIState guiState, int index)
        {
            if (right != null)
                return right(index);

            return base.GetRight(guiState,index);
        }

        /// <summary>
        /// Override for GetUserData
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Return the user data</returns>
        protected override object GetUserData(IGUIState guiState, int index)
        {
            if (userData != null)
                return userData(index);

            return base.GetUserData(guiState,index);
        }
    }
}
