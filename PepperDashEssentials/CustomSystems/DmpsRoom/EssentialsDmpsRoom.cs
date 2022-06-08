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