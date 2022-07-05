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
using CI.Essentials.Video;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambers : EssentialsRoomBase, IEssentialsCouncilChambers
    {
        public EssentialsCouncilChambersPropertiesConfig PropertiesConfig { get; private set; }

        public AudioController audio { get; private set; }
        public VideoController video { get; private set; }

        /// <summary>
        /// Timer used for informing the UIs of a shutdown
        /// </summary>        

        public SecondsCountdownTimer PowerChangingTimer { get; private set; }
        public int PowerOffSeconds { get; private set; }
        public int WarmUpSeconds { get; private set; }

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
                video = new VideoController();
                
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
                //PowerChangingTimer = new SecondsCountdownTimer(Key + "-powering-timer");
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Error Initializing Room: {0}", e);
            }
        }

        private void ShutDownComplete()
        {
        }
        #endregion // constructor

        void ShutdownPromptTimer_HasStarted(object sender, EventArgs e)
        {
            Debug.Console(0, this, "ShutdownPromptTimer_HasStarted: {0}", e);
        }

        /// <summary>
        /// 
        /// </summary>
        //void CancelPowerOffTimer()
        //{
        //    Debug.Console(1, "{0}, CancelPowerOffTimer", classname);
        //    if (PowerOffTimer != null)
        //    {
        //        PowerOffTimer.Stop();
        //        PowerOffTimer = null;
        //    }
        //}

        #region EssentialsRoomBase implementation

        protected override void EndShutdown()
        {
            Debug.Console(1, this, "EndShutdown");
            SetDefaultLevels();
            //if (!PowerChangingTimer.IsRunningFeedback.BoolValue)
            //{
            //    Debug.Console(1, this, "Starting PowerChangingTimer");
            //    PowerChangingTimer.Start();
            //}
        }

        protected override Func<bool> IsCoolingFeedbackFunc
        {
            get
            {
                Debug.Console(1, this, "IsCoolingFeedbackFunc {0}", ShutdownPromptTimer.IsRunningFeedback.BoolValue);
                return () => { return ShutdownPromptTimer.IsRunningFeedback.BoolValue; };
                //return () => { return false; };
            }
        }

        protected override Func<bool> IsWarmingFeedbackFunc
        {
            get
            {
                Debug.Console(1, this, "IsWarmingFeedbackFunc");
                return () => { return false; };
            }
        }

        protected override Func<bool> OnFeedbackFunc
        {
            get {
                //OnFeedback = new BoolFeedback(() => { return true; });
                return () => 
                {
                    Debug.Console(1, this, "OnFeedbackFunc: {0}", OnFeedback.BoolValue);
                    //return OnFeedback.BoolValue;
                    return true;
                }; 
            }
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