using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// 
    /// </summary>
    public class ModeChangeEventArgs : EventArgs
    {
        public IBasicModeControls OldDev { get; private set; }
        public IBasicModeControls NewDev { get; private set; }
        public ChangeType Type { get; private set; }

        public ModeChangeEventArgs(IBasicModeControls oldDev, IBasicModeControls newDev, ChangeType type)
        {
            OldDev = oldDev;
            NewDev = newDev;
            Type = type;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ChangeType
    {
        WillChange, DidChange
    }
}