using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

using CI.Essentials.Audio;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambers : EssentialsRoomBase, IEssentialsCouncilChambers
    {
        public EssentialsCouncilChambersPropertiesConfig PropertiesConfig { get; private set; }

        public AudioController audio { get; private set; }

        #region constructor

        public EssentialsCouncilChambers(DeviceConfig config)
            : base(config)
        {
            try
            {
                Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                Debug.Console(1, this, "Creating EssentialsCouncilChambers");
                PropertiesConfig = JsonConvert.DeserializeObject<EssentialsCouncilChambersPropertiesConfig>
                    (config.Properties.ToString());

                audio = new AudioController(PropertiesConfig);

                InitializeRoom();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        private void InitializeRoom()
        {
            try
            {
                Debug.Console(1, this, "InitializeRoom");
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Error Initializing Room: {0}", e);
            }
        }

        #endregion // constructor

        #region EssentialsRoomBase implementation

        protected override void EndShutdown()
        {
            SetDefaultLevels();
        }

        protected override Func<bool> IsCoolingFeedbackFunc
        {
            get { return () => { return false; }; }
        }

        protected override Func<bool> IsWarmingFeedbackFunc
        {
            get { return () => { return false; }; }
        }

        protected override Func<bool> OnFeedbackFunc
        {
            get { return () => { return false; }; }
        }

        public override void PowerOnToDefaultOrLastSource()
        {
            Debug.Console(1, this, "PowerOnToDefaultOrLastSource");
        }

        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.Console(1, this, "RoomVacatedForTimeoutPeriod {0}", o.ToString());
        }

        public override bool RunDefaultPresentRoute()
        {
            Debug.Console(1, this, "RunDefaultPresentRoute");
            return false;
        }

        public override void SetDefaultLevels()
        {
            Debug.Console(1, this, "Restoring default levels");
        }

        #endregion //EssentialsRoomBase implementation
    }
}