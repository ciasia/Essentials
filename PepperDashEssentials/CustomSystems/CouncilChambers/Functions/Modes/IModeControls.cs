using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace CI.Essentials.Modes
{
    public interface IModeControls : IHasCurrentModeInfoChange
    {
        //ModeSingleControlManager SystemModesControl { get; set; }
        event EventHandler<ModeChangeEventArgs> ModeChange;

        Dictionary<string, ModeSingleControlManager> ModesControlList { get; set; }
        event EventHandler<KeyedModeChangeEventArgs> ModesDeviceListChange;
    }
}