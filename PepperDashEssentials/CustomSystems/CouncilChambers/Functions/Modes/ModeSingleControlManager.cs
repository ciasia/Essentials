using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace CI.Essentials.Modes
{
    public class KeyedModeChangeEventArgs : ModeChangeEventArgs
    {
        public string Key { get; private set; }

        public KeyedModeChangeEventArgs(string key, IBasicModeControls oldDev, IBasicModeControls newDev, ChangeType type)
            : base(oldDev, newDev, type)
        {
            Key = Key;
        }
    }

    public class ModeSingleControlManager: IKeyed
    {
        public string Key { get; private set; }
        public event EventHandler<KeyedModeChangeEventArgs> CurrentDeviceChange;

        public IBasicModeControls DefaultDevice { get; private set; }
        public IBasicModeControls DefaultControl { get; private set; }

        private IBasicModeControls _CurrentDevice;
        public IBasicModeControls CurrentControl
        {
            get { return _CurrentDevice; }
            set
            {
                if (value == _CurrentDevice) return;

                var oldDev = _CurrentDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "mode");
                var handler = CurrentDeviceChange;
                if (handler != null)
                    CurrentDeviceChange(this, new KeyedModeChangeEventArgs(Key, oldDev, value, ChangeType.WillChange));
                _CurrentDevice = value;
                if (handler != null)
                    CurrentDeviceChange(this, new KeyedModeChangeEventArgs(Key, oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentDevice is IInUseTracking)
                    (_CurrentDevice as IInUseTracking).InUseTracker.AddUser(this, "mode");
            }
        }


        public ModeSingleControlManager(string key, IBasicModeControls defaultDevice)
        {
            Key = key;
            DefaultDevice = defaultDevice;
        }

        public void Initialize()
        {
            if (DefaultDevice is IBasicModeControls)
                DefaultControl = DefaultDevice as IBasicModeControls;
            //else if (DefaultDevice is IHasModeDevice)
            //    DefaultControl = (DefaultDevice as IHasModeDevice).ModeDevice;
            else
                Debug.Console(1, "DefaultModeControls {0} not set", Key);
            CurrentControl = DefaultControl;
        }
    }
}