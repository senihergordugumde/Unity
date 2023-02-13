using System;
using UnityEngine;

namespace UnityEditor.U2D.Path.GUIFramework
{
    /// <summary>
    /// Represents a default generic UI control.
    /// </summary>
    public class GenericDefaultControl : DefaultControl
    {
        /// <summary>
        /// Func for GetPosition
        /// </summary>
        public Func<IGUIState, Vector3> position;
        /// <summary>
        /// Func for GetForward
        /// </summary>
        public Func<IGUIState, Vector3> forward;
        /// <summary>
        /// Func for GetUp
        /// </summary>
        public Func<IGUIState, Vector3> up;
        /// <summary>
        /// Func for GetRight
        /// </summary>
        public Func<IGUIState, Vector3> right;
        /// <summary>
        /// Func for GetUserData
        /// </summary>
        public Func<IGUIState, object> userData;

        /// <summary>
        /// Initializes and returns an instance of GenericDefaultControl
        /// </summary>
        /// <param name="name">The name of the generic default control.</param>
        public GenericDefaultControl(string name) : base(name)
        {
        }

        /// <summary>
        /// Gets the distance from the Scene view camera to the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>The distance from the Scene view camera to the control.</returns>
        protected override Vector3 GetPosition(IGUIState guiState, int index)
        {
            if (position != null)
                return position(guiState);

            return base.GetPosition(guiState, index);
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
                return forward(guiState);

            return base.GetForward(guiState, index);
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
                return up(guiState);

            return base.GetUp(guiState, index);
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
                return right(guiState);

            return base.GetRight(guiState, index);
        }

        /// <summary>
        /// Gets the control's user data. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The Index</param>
        /// <returns>Returns the user data</returns>
        protected override object GetUserData(IGUIState guiState, int index)
        {
            if (userData != null)
                return userData(guiState);

            return base.GetUserData(guiState, index);
        }
    }
}
