using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace PepperDash.Essentials.DspRoom
{
    public class KeyedVolumeDeviceChangeEventArgs : VolumeDeviceChangeEventArgs
    {
        public string Key { get; private set; }

        public KeyedVolumeDeviceChangeEventArgs(string key, IBasicVolumeControls oldDev, IBasicVolumeControls newDev, ChangeType type)
            :base(oldDev, newDev, type)
        {
            Key = Key;
        }
    }

    public class AudioDeviceSingleControlManager
    {
        // All classes named *Control are actually *VolumeControls

        public string Key { get; private set; }
        public event EventHandler<KeyedVolumeDeviceChangeEventArgs> CurrentDeviceChange;

        public IBasicVolumeControls DefaultDevice { get; private set; }
        public IBasicVolumeControls DefaultControl { get; private set; }

        private IBasicVolumeControls _CurrentDevice;
        public IBasicVolumeControls CurrentControl
        {
            get { return _CurrentDevice; }
            set
            {
                if (value == _CurrentDevice) return;

                var oldDev = _CurrentDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "audio");
                var handler = CurrentDeviceChange;
                if (handler != null)
                    CurrentDeviceChange(this, new KeyedVolumeDeviceChangeEventArgs(Key, oldDev, value, ChangeType.WillChange));
                _CurrentDevice = value;
                if (handler != null)
                    CurrentDeviceChange(this, new KeyedVolumeDeviceChangeEventArgs(Key, oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentDevice is IInUseTracking)
                    (_CurrentDevice as IInUseTracking).InUseTracker.AddUser(this, "audio");
            }
        }


        public AudioDeviceSingleControlManager(string key, IBasicVolumeControls defaultDevice)
        {
            Key = key;
            DefaultDevice = defaultDevice;
        }

        public void Initialize()
        {
            if (DefaultDevice is IBasicVolumeControls)
                DefaultControl = DefaultDevice as IBasicVolumeControls;
            else if (DefaultDevice is IHasVolumeDevice)
                DefaultControl = (DefaultDevice as IHasVolumeDevice).VolumeDevice;
            else
                Debug.Console(1, "DefaultVolumeControls {0} not set", Key);
            CurrentControl = DefaultControl;
        }
    }
}