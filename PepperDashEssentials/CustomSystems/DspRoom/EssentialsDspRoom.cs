using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Room.Config;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using PepperDash.Essentials.Devices.Common.AudioCodec;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace PepperDash.Essentials.DspRoom
{
    public class EssentialsDspRoom : EssentialsRoomBase, IEssentialsDspRoom
    {
        public EssentialsDspRoomPropertiesConfig PropertiesConfig { get; private set; }

        public AudioDeviceSingleControlManager MasterVolumeControl { get; set; }
        public event EventHandler<VolumeDeviceChangeEventArgs> MasterVolumeDeviceChange;
        
        public Dictionary<string, AudioDeviceSingleControlManager> VolumeControlList { get; set; }
        public event EventHandler<KeyedVolumeDeviceChangeEventArgs> VolumeDeviceListChange;

        public EssentialsDspRoom(DeviceConfig config)
            : base(config)
        {
            try
            {
                Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                Debug.Console(1, this, "Creating EssentialsDspRoom");
                PropertiesConfig = JsonConvert.DeserializeObject<EssentialsDspRoomPropertiesConfig>
                    (config.Properties.ToString());

                var device_ = DeviceManager.GetDeviceForKey(PropertiesConfig.DefaultAudioKey);
                //Debug.Console(1, this, "device_ {0}= null", device_ == null ? "=" : "!");
                // add the device for the main volume control slider
                MasterVolumeControl = new AudioDeviceSingleControlManager(PropertiesConfig.DefaultAudioKey, device_ as IBasicVolumeControls);
                MasterVolumeControl.CurrentDeviceChange += new EventHandler<KeyedVolumeDeviceChangeEventArgs>(MasterFader_CurrentDeviceChange);
                Debug.Console(1, this, "Added MasterVolumeControl {0}", MasterVolumeControl.Key);

                VolumeControlList = new Dictionary<string, AudioDeviceSingleControlManager>();
                foreach (var d in PropertiesConfig.VolumeList)
                {
                    var key_ = d.Value.LevelKey;
                    var dev_ = DeviceManager.GetDeviceForKey(key_);
                    var control_ = new AudioDeviceSingleControlManager(key_, dev_ as IBasicVolumeControls);
                    VolumeControlList.Add(key_, control_);
                    control_.CurrentDeviceChange += new EventHandler<KeyedVolumeDeviceChangeEventArgs>(VolumeControlList_CurrentDeviceChange);
                    Debug.Console(1, this, "Added VolumeControlList {0} {1}", control_.Key, key_);
                    //Debug.Console(1, this, "VolumeControlList[{0}].CurrentControl {1}= null", key_, VolumeControlList[key_].CurrentControl == null ? "=" : "!");
                }                
                InitializeRoom();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        void VolumeControlList_CurrentDeviceChange(object sender, KeyedVolumeDeviceChangeEventArgs e)
        {
            Debug.Console(1, this, "VolumeControlList_CurrentDeviceChange {0}", e.Key);
            //var dev_ = DeviceManager.GetDeviceForKey(e.Key);
        }

        void MasterFader_CurrentDeviceChange(object sender, KeyedVolumeDeviceChangeEventArgs e)
        {
            Debug.Console(1, this, "MasterFader_CurrentDeviceChange {0}", e.Key);
            if (MasterVolumeDeviceChange != null)
                MasterVolumeDeviceChange(this, e);
        }

        public void InitializeRoom()
        {
            try
            {
                Debug.Console(1, this, "InitializeRoom");
                if (MasterVolumeControl.DefaultDevice == null)
                {
                    Debug.Console(1, this, "AllDevices as IBasicVolumeControls");
                    foreach (var d in DeviceManager.AllDevices)
                        if (d is IBasicVolumeControls)
                            Debug.Console(1, this, "{0}", d.Key);
                }
                else
                    Debug.Console(1, this, "DefaultAudioDevice {0}", PropertiesConfig.DefaultAudioKey);
                
                MasterVolumeControl.Initialize();
                foreach (var v in VolumeControlList)
                    v.Value.Initialize();
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Error Initializing Room: {0}", e);
            }
        }

        protected override void CustomSetConfig(DeviceConfig config)
        {
            var newPropertiesConfig = JsonConvert.DeserializeObject<EssentialsDspRoomPropertiesConfig>(config.Properties.ToString());

            if (newPropertiesConfig != null)
                PropertiesConfig = newPropertiesConfig;

            ConfigWriter.UpdateRoomConfig(config);
        }

        public override bool CustomActivate()
        {
            Debug.Console(1, this, "CustomActivate -- Volumes");
            this.DefaultVolume = (ushort)(PropertiesConfig.Volumes.Master.Level * 65535 / 100);
            //Debug.Console(1, this, "DefaultVolume {0}", DefaultVolume);

            Debug.Console(1, this, "base.CustomActivate");
            return base.CustomActivate();
        }

        /// <summary>
        /// Runs "roomOff" action on all rooms not set to ExcludeFromGlobalFunctions
        /// </summary>
        public static void AllRoomsOff()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndShutdown()
        {
            SetDefaultLevels();
        }


        #region audio

        public ushort DefaultVolume { get; set; }

        /// <summary>
        /// Does what it says
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.Console(1, this, "Restoring default levels");
            var vc = MasterVolumeControl.CurrentControl as IBasicVolumeWithFeedback;
            if (vc != null)
                vc.SetVolume(DefaultVolume);
            foreach (var v in VolumeControlList)
            {
                vc = v.Value.CurrentControl as IBasicVolumeWithFeedback;
                if (vc != null)
                    vc.SetVolume(DefaultVolume);
            }
        }

        #endregion

        #region IHasCurrentLevelInfoChange Members

        // not sure if these are actually necessary
        
        //public string CurrentLevelInfoKey { get; set; }

        /// <summary>
        /// The LevelListItem last run - containing names and icons 
        /// </summary>
        public LevelListItem CurrentLevelInfo
        {
            get { return _CurrentLevelInfo; }
            set
            {
                Debug.Console(0, this, "Setting CurrentLevelInfo: {0}", value.LevelKey);
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
            if (!string.IsNullOrEmpty(PropertiesConfig.VolumeListKey))
            {
                SetVolumeListKey(PropertiesConfig.VolumeListKey);
            }
            else
            {
                SetVolumeListKey(Key);
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

        #region implement EssentialsRoomBase

        protected override Func<bool> IsWarmingFeedbackFunc
        {
            get
            {
                //throw new NotImplementedException(); 
                return () => { return false; };
            }
        }

        protected override Func<bool> IsCoolingFeedbackFunc
        {
            get
            {
                //throw new NotImplementedException(); 
                return () => { return false; };
            }
        }

        protected override Func<bool> OnFeedbackFunc
        {
            get
            {
                //throw new NotImplementedException(); 
                return () => { return false; };
            }
        }

        public override void PowerOnToDefaultOrLastSource()
        {
            //throw new NotImplementedException();
        }

        public override bool RunDefaultPresentRoute()
        {
            //throw new NotImplementedException();
            return false;
        }

        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            //throw new NotImplementedException();
        }

        #endregion //implement EssentialsRoomBase
    }
}