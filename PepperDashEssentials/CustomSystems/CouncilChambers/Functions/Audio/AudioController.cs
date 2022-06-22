using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using CI.Essentials.Levels;

namespace CI.Essentials.Audio
{
    public class AudioController : IHasCurrentLevelInfoChange, IAudioControls
    {
        public AudioDeviceSingleControlManager MasterVolumeControl { get; set; }
        public event EventHandler<VolumeDeviceChangeEventArgs> MasterVolumeDeviceChange;
        
        public Dictionary<string, AudioDeviceSingleControlManager> VolumeControlList { get; set; }
        public event EventHandler<KeyedVolumeDeviceChangeEventArgs> VolumeDeviceListChange;

        public IAudioPropertiesConfig config { get; private set; }

        public IKeyed default_device { get; private set; }

        public AudioController(IAudioPropertiesConfig config)
        {
            this.config = config;
            default_device = DeviceManager.GetDeviceForKey(config.DefaultAudioKey);

            MasterVolumeControl = new AudioDeviceSingleControlManager(config.DefaultAudioKey, default_device as IBasicVolumeControls);
            MasterVolumeControl.CurrentDeviceChange += new EventHandler<KeyedVolumeDeviceChangeEventArgs>(MasterFader_CurrentDeviceChange);
            Debug.Console(1, default_device, "Added MasterVolumeControl");

            VolumeControlList = new Dictionary<string, AudioDeviceSingleControlManager>();
            foreach (var d in config.VolumeList)
            {
                var key_ = d.Value.LevelKey;
                var dev_ = DeviceManager.GetDeviceForKey(key_);
                var control_ = new AudioDeviceSingleControlManager(key_, dev_ as IBasicVolumeControls);
                VolumeControlList.Add(key_, control_);
                control_.CurrentDeviceChange += new EventHandler<KeyedVolumeDeviceChangeEventArgs>(VolumeControlList_CurrentDeviceChange);
                Debug.Console(1, dev_, "Added VolumeControlList");
                //Debug.Console(1, this, "VolumeControlList[{0}].CurrentControl {1}= null", key_, VolumeControlList[key_].CurrentControl == null ? "=" : "!");
            }
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                Debug.Console(1, default_device, "InitializeRoom");
                if (MasterVolumeControl.DefaultDevice == null)
                {
                    Debug.Console(1, default_device, "AllDevices as IBasicVolumeControls");
                    foreach (var d in DeviceManager.AllDevices)
                        if (d is IBasicVolumeControls)
                            Debug.Console(1, default_device, "{0}", d.Key);
                }
                else
                    Debug.Console(1, default_device, "DefaultAudioDevice {0}", config.DefaultAudioKey);

                MasterVolumeControl.Initialize();
                foreach (var v in VolumeControlList)
                    v.Value.Initialize();
            }
            catch (Exception e)
            {
                Debug.Console(0, default_device, "Error Initializing Room: {0}", e);
            }
        }

        void VolumeControlList_CurrentDeviceChange(object sender, KeyedVolumeDeviceChangeEventArgs e)
        {
            Debug.Console(1, default_device, "VolumeControlList_CurrentDeviceChange {0}", e.Key);
            //var dev_ = DeviceManager.GetDeviceForKey(e.Key);
        }

        void MasterFader_CurrentDeviceChange(object sender, KeyedVolumeDeviceChangeEventArgs e)
        {
            Debug.Console(1, default_device, "MasterFader_CurrentDeviceChange {0}", e.Key);
            if (MasterVolumeDeviceChange != null)
                MasterVolumeDeviceChange(this, e);
        }


        #region IHasCurrentLevelInfoChange Members

        /// <summary>
        /// The LevelListItem last run - containing names and icons 
        /// </summary>
        public LevelListItem CurrentLevelInfo
        {
            get { return _CurrentLevelInfo; }
            set
            {
                Debug.Console(0, default_device, "Setting CurrentLevelInfo: {0}", value.LevelKey);
                if (value == _CurrentLevelInfo) return;

                var handler = CurrentLevelChange;
                // remove from in-use tracker, if so equipped
                if (_CurrentLevelInfo != null && _CurrentLevelInfo.LevelDevice is IInUseTracking)
                    (_CurrentLevelInfo.LevelDevice as IInUseTracking).InUseTracker.RemoveUser(this, "control");

                if (handler != null)
                    handler(_CurrentLevelInfo, ChangeType.WillChange);

                _CurrentLevelInfo = value;

                // add to in-use tracking
                if (_CurrentLevelInfo != null && _CurrentLevelInfo.LevelDevice is IInUseTracking)
                    (_CurrentLevelInfo.LevelDevice as IInUseTracking).InUseTracker.AddUser(this, "control");
                if (handler != null)
                    handler(_CurrentLevelInfo, ChangeType.DidChange);
            }
        }
        LevelListItem _CurrentLevelInfo;

        public event LevelInfoChangeHandler CurrentLevelChange;

        /// <summary>
        /// Sets the VolumeListKey property to the passed in value or the default if no value passed in
        /// </summary>
        /// <param name="sourceListKey"></param>
        protected void SetVolumeListKey(string volumeListKey)
        {
            if (!string.IsNullOrEmpty(volumeListKey))
            {
                VolumeListKey = volumeListKey;
            }
            else
            {
                volumeListKey = _defaultVolumeListKey;
            }
        }
        private void SetVolumeListKey()
        {
            if (!string.IsNullOrEmpty(config.VolumeListKey))
            {
                SetVolumeListKey(config.VolumeListKey);
            }
            else
            {
                Debug.Console(1, default_device, "AudioController SetVolumeListKey is null");
                SetVolumeListKey(default_device.Key);
            }

        }

        /// <summary>
        /// The config name of the source list
        /// </summary>
        /// 
        protected string _VolumeListKey;
        public string VolumeListKey
        {
            get
            {
                return _VolumeListKey;
            }
            private set
            {
                if (value != _VolumeListKey)
                {
                    _VolumeListKey = value;
                }
            }
        }

        protected const string _defaultVolumeListKey = "default";

        #endregion
    }
}