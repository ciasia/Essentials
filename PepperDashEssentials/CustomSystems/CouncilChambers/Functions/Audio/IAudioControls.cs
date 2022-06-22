using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using CI.Essentials.Levels;

namespace CI.Essentials.Audio
{
    public interface IAudioControls : IHasCurrentLevelInfoChange
    {
        AudioDeviceSingleControlManager MasterVolumeControl { get; set; }
        event EventHandler<VolumeDeviceChangeEventArgs> MasterVolumeDeviceChange;

        Dictionary<string, AudioDeviceSingleControlManager> VolumeControlList { get; set; }
        event EventHandler<KeyedVolumeDeviceChangeEventArgs> VolumeDeviceListChange;
    }
}