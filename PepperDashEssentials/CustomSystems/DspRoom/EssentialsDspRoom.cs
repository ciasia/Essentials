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
        
        public EssentialsDspRoom(DeviceConfig config)
            : base(config)
        {
            try
            {
                PropertiesConfig = JsonConvert.DeserializeObject<EssentialsDspRoomPropertiesConfig>
                    (config.Properties.ToString());

                var device_ = DeviceManager.GetDeviceForKey(PropertiesConfig.DefaultAudioKey);
                //Debug.Console(1, this, "device_ {0}= null", device_ == null ? "=" : "!");
                DefaultAudioDevice = DeviceManager.GetDeviceForKey(PropertiesConfig.DefaultAudioKey) as IBasicVolumeControls;
                //Debug.Console(1, this, "DefaultAudioDevice {0}= null", DefaultAudioDevice == null ? "=" : "!");
                InitializeRoom();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        public void InitializeRoom()
        {
            try
            {
                Debug.Console(1, this, "InitializeRoom");
                if (DefaultAudioDevice == null)
                {
                    Debug.Console(1, this, "AllDevices as IBasicVolumeControls");
                    foreach (var d in DeviceManager.AllDevices)
                        if (d is IBasicVolumeControls)
                            Debug.Console(1, this, "{0}", d.Key);
                }
                else
                    Debug.Console(1, this, "DefaultAudioDevice {0}", PropertiesConfig.DefaultAudioKey);

                if (DefaultAudioDevice is IBasicVolumeControls)
                    DefaultVolumeControls = DefaultAudioDevice as IBasicVolumeControls;
                else if (DefaultAudioDevice is IHasVolumeDevice)
                    DefaultVolumeControls = (DefaultAudioDevice as IHasVolumeDevice).VolumeDevice;
                else
                    Debug.Console(1, this, "DefaultVolumeControls not set");
                CurrentVolumeControls = DefaultVolumeControls;
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
            //Debug.Console(1, this, "PropertiesConfig {0}= null", PropertiesConfig == null ? "=" : "!");
            //Debug.Console(1, this, "PropertiesConfig.Volumes {0}= null", PropertiesConfig.Volumes == null ? "=" : "!");
            //Debug.Console(1, this, "PropertiesConfig.Volumes.Master {0}= null", PropertiesConfig.Volumes.Master == null ? "=" : "!");
            //Debug.Console(1, this, "PropertiesConfig.Volumes.Master.Level: {0}", PropertiesConfig.Volumes.Master.Level);
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

        #region audio

        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;

        public IBasicVolumeControls DefaultAudioDevice { get; private set; }
        public IBasicVolumeControls DefaultVolumeControls { get; private set; }

        public ushort DefaultVolume { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IBasicVolumeControls CurrentVolumeControls
        {
            get { return _CurrentAudioDevice; }
            set
            {
                if (value == _CurrentAudioDevice) return;

                var oldDev = _CurrentAudioDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "audio");
                var handler = CurrentVolumeDeviceChange;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.WillChange));
                _CurrentAudioDevice = value;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentAudioDevice is IInUseTracking)
                    (_CurrentAudioDevice as IInUseTracking).InUseTracker.AddUser(this, "audio");
            }
        }
        IBasicVolumeControls _CurrentAudioDevice;

        /// <summary>
        /// Does what it says
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.Console(1, this, "Restoring default levels");
            var vc = CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (vc != null)
                vc.SetVolume(DefaultVolume);
        }
        
        #endregion

    }
}