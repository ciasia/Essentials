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
using PepperDash.Essentials.DM;

namespace PepperDash.Essentials.DmpsRoom
{
    public class EssentialsDmpsRoom : EssentialsRoomBase, IEssentialsDmpsRoom
    {

        public EssentialsRoomPropertiesConfig PropertiesConfig { get; private set; }
        
        public EssentialsDmpsRoom(DeviceConfig config)
            : base(config)
        {
            try
            {
                Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                Debug.Console(1, this, "Creating EssentialsDmpsRoom");
                PropertiesConfig = JsonConvert.DeserializeObject<EssentialsDmpsRoomPropertiesConfig>
                    (config.Properties.ToString());
                
                Initialize();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        public override void Initialize()
        {
            try
            {
                var cs_ = DeviceManager.GetDeviceForKey("processor-avRouting");
                Debug.Console(0, this, "controlsystem.type: {0}", cs_.ToString());
                if (cs_ is DmpsRoutingController)
                {
                    Debug.Console(0, this, "controlsystem is DmpsRoutingController");
                    var router_ = cs_ as DmpsRoutingController;
                    Debug.Console(0, this, "InputNames");
                    foreach (var i in router_.InputNames)
                    {
                        Debug.Console(0, this, "InputNames [{0}] {1}", i.Key, i.Value);
                    }
                    Debug.Console(0, this, "OutputNames");
                    foreach (var i in router_.OutputNames)
                    {
                        Debug.Console(0, this, "OutputNames [{0}] {1}", i.Key, i.Value);
                    }

                    Debug.Console(0, this, "InputPorts");
                    foreach (var i in router_.InputPorts)
                    {
                        Debug.Console(0, this, "InputPorts key: {0}", i.Key);
                        Debug.Console(0, this, "InputPorts Port.ToString: {0}", i.Port.ToString());
                    }
                    Debug.Console(0, this, "OutputPorts");
                    foreach (var o in router_.OutputPorts)
                    {
                        Debug.Console(0, this, "OutputPorts key: {0}", o.Key);
                        Debug.Console(0, this, "OutputPorts Port.ToString: {0}", o.Port.ToString());
                    }

                    Debug.Console(0, this, "VolumeControls");
                    foreach (var o in router_.VolumeControls)
                    {
                        Debug.Console(0, this, "VolumeControls key: {0}", o.Key);
                        Debug.Console(0, this, "VolumeControls Output.Number: {0}", o.Value.Output.Number);
                        Debug.Console(0, this, "VolumeControls OutputVolume.Name: {0}", o.Value.Output.Volume.Name);
                        Debug.Console(0, this, "VolumeControls OutputVolume.Number: {0}", o.Value.Output.Volume.Number);
                    }

                    Debug.Console(0, this, "Microphones {0}", router_.Microphones);

                }
                else
                    Debug.Console(0, this, "controlsystem is NOT DmpsRoutingController");

            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Error Initializing Room: {0}", e);
            }
        }

        protected override void CustomSetConfig(DeviceConfig config)
        {
            var newPropertiesConfig = JsonConvert.DeserializeObject<EssentialsDmpsRoomPropertiesConfig>(config.Properties.ToString());

            if (newPropertiesConfig != null)
                PropertiesConfig = newPropertiesConfig;

            ConfigWriter.UpdateRoomConfig(config);
        }


        public override bool CustomActivate()
        {
            return base.CustomActivate();
        }

        /// <summary>
        /// Runs "roomOff" action on all rooms not set to ExcludeFromGlobalFunctions
        /// </summary>
        public static void AllRoomsOff()
        {
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

        protected override void EndShutdown()
        {
            //throw new NotImplementedException();
        }

        public override void SetDefaultLevels()
        {
            //throw new NotImplementedException();
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