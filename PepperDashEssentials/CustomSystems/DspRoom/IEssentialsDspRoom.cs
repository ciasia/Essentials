using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace PepperDash.Essentials.DspRoom
{
    public interface IEssentialsDspRoom : IEssentialsRoom, IHasCurrentLevelInfoChange
    {
        EssentialsDspRoomPropertiesConfig PropertiesConfig { get; }

        AudioDeviceSingleControlManager MasterVolumeControl { get; set; }
        event EventHandler<VolumeDeviceChangeEventArgs> MasterVolumeDeviceChange;

        Dictionary<string, AudioDeviceSingleControlManager> VolumeControlList { get; set; }
        event EventHandler<KeyedVolumeDeviceChangeEventArgs> VolumeDeviceListChange;
    }
}