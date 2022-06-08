using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace PepperDash.Essentials.DspRoom
{
    public interface IEssentialsDspRoom : IEssentialsRoom
    {
        EssentialsDspRoomPropertiesConfig PropertiesConfig { get; }

        IBasicVolumeControls CurrentVolumeControls { get; }
        event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
    }
}