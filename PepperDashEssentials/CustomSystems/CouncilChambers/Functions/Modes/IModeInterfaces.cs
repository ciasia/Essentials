using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// Defines minimal Mode control methods
    /// </summary>
    public interface IBasicModeControls
    {
        void ModeChange(bool pressRelease);
    }

    /// <summary>
    /// Adds feedback and direct Mode set to IBasicModeControls
    /// </summary>
    public interface IBasicModeWithFeedback : IBasicModeControls
    {
        BoolFeedback ModeFeedback { get; }
    }

    /// <summary>
    /// A class that implements this contains a reference to a current IBasicModeControls device.
    /// The class may have multiple IBasicModeControls.
    /// </summary>
    public interface IHasCurrentModeControls
    {
        IBasicModeControls CurrentModeControls { get; }
        event EventHandler<ModeChangeEventArgs> CurrentModeChange;
    }

}