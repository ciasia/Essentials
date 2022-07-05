using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using CI.Essentials.Utilities;

namespace CI.Essentials.Power
{
    public class ShutdownFunctionDriver : PanelDriverBase, IEssentialsConnectableRoomDriver, IKeyed
    {
        //CrestronTouchpanelPropertiesConfig Config;
        string IKeyed.Key { get { return "ShutDownUIDriver"; } }
        //string Key = "ShutDownUIDriver";

        public uint StartPageVisibleJoin { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        IEssentialsRoom _CurrentRoom;

        /// <summary>
        /// 
        /// </summary>
        public uint PowerOffTimeout { get; set; }

        /// <summary>
        /// Will auto-timeout a power off
        /// </summary>
        //CTimer PowerOffTimer;

        /// <summary>
        /// Controls timeout of notification ribbon timer
        /// </summary>
        CTimer RibbonTimer;

        ModalDialog PowerDownModal;
        BoolInputSig EndMeetingButtonSig;

        public ShutdownFunctionDriver(PanelDriverBase parent)//, CrestronTouchpanelPropertiesConfig config) 
			: base(parent.TriList)
		{
            Debug.Console(1, this, "=====================================");
            Debug.Console(1, this, "Loading");
            //Config = config;
            //Parent = parent;
            PowerOffTimeout = 5000;
            StartPageVisibleJoin = UIBoolJoin.StartPageVisible; //  this may be changed externally
            //EndMeetingButtonSig.BoolValue = true; // this may also change externally

            Initialise();
            Debug.Console(1, this, "=====================================");
        }

        private void Initialise()
        {
            Debug.Console(1, this, "Initialise");
            TriList.SetSigFalseAction(UIBoolJoin.ShowPowerOffPress, PowerButtonPressed);

            TriList.SetSigFalseAction(UIBoolJoin.DisplayPowerTogglePress, () =>
            {
                //if (CurrentRoom != null)
                //    (CurrentRoom.PowerToggle();
            });
        } 
       
		/// <summary>
		/// 
		/// </summary>
        public override void Show()
        {
            Debug.Console(1, this, "Show");
            base.Show();
        }


        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "DisconnectCurrentRoom");
            _CurrentRoom = room;
            if (_CurrentRoom != null)
            {
                // Disconnect current room
                _CurrentRoom.ShutdownPromptTimer.HasStarted -= ShutdownPromptTimer_HasStarted;
                _CurrentRoom.ShutdownPromptTimer.HasFinished -= ShutdownPromptTimer_HasFinished;
                _CurrentRoom.ShutdownPromptTimer.WasCancelled -= ShutdownPromptTimer_WasCancelled;
                _CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.OutputChange -= IsRunningFeedback_OutputChange;

                _CurrentRoom.OnFeedback.OutputChange -= CurrentRoom_OnFeedback_OutputChange;
                _CurrentRoom.IsWarmingUpFeedback.OutputChange -= CurrentRoom_IsWarmingFeedback_OutputChange;
                _CurrentRoom.IsCoolingDownFeedback.OutputChange -= IsCoolingDownFeedback_OutputChange;
            }
        }


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            _CurrentRoom = room;
            Debug.Console(1, this, "_CurrentRoom".IsNullString(_CurrentRoom));

            if (_CurrentRoom != null)
            {
                // Shutdown timer
                Debug.Console(1, this, "subscribing to ShutdownPromptTimer");
                _CurrentRoom.ShutdownPromptTimer.HasStarted += ShutdownPromptTimer_HasStarted;
                _CurrentRoom.ShutdownPromptTimer.HasFinished += ShutdownPromptTimer_HasFinished;
                _CurrentRoom.ShutdownPromptTimer.WasCancelled += ShutdownPromptTimer_WasCancelled;
                _CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.OutputChange += IsRunningFeedback_OutputChange;

                // Link up all the change events from the room
                Debug.Console(1, this, "subscribing to OnFeedback");
                _CurrentRoom.OnFeedback.OutputChange += CurrentRoom_OnFeedback_OutputChange;
                CurrentRoom_SyncOnFeedback();
                Debug.Console(1, this, "subscribing to IsWarmingUpFeedback");
                _CurrentRoom.IsWarmingUpFeedback.OutputChange += CurrentRoom_IsWarmingFeedback_OutputChange;
                Debug.Console(1, this, "subscribing to IsCoolingDownFeedback");
                _CurrentRoom.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange;
            }
        }

        void IsRunningFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(1, this, "IsRunningFeedback_OutputChange {0}", e.BoolValue);
            // could show dialog here but it should be working from the modaldialog itself
        }


        /// <summary>
        /// 
        /// </summary>
        public void PowerButtonPressed()
        {
            Debug.Console(1, this, "PowerButtonPressed");
            Debug.Console(1, this, "_CurrentRoom.OnFeedback {0}", _CurrentRoom.OnFeedback.BoolValue);
            Debug.Console(1, this, "_CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue {0}", _CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue);
            //if (!_CurrentRoom.OnFeedback.BoolValue
            //    || _CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue)
            //    return;
            if (_CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue)
                return;
            Debug.Console(1, this, "PowerButtonPressed StartShutdown");

            _CurrentRoom.StartShutdown(eShutdownType.Manual);
        }

        void CreateModalDialog()
        {
            Debug.Console(1, this, "Creating ModalDialog");
            var timer = _CurrentRoom.ShutdownPromptTimer;

            PowerDownModal = new ModalDialog(TriList);
            var message = string.Format("System will power off in {0} seconds", _CurrentRoom.ShutdownPromptSeconds);

            // Attach timer things to modal
            _CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange += ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            _CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange += ShutdownPromptTimer_PercentFeedback_OutputChange;

            // respond to offs by cancelling dialog
            var onFb = _CurrentRoom.OnFeedback;
            EventHandler<FeedbackEventArgs> offHandler = null;
            offHandler = (o, a) =>
            {
                Debug.Console(1, this, "ModalDialog offHandler onFb: {0}", onFb.BoolValue);
                if (!onFb.BoolValue)
                {
                    EndMeetingButtonSig.BoolValue = false;
                    PowerDownModal.HideDialog();
                    onFb.OutputChange -= offHandler;
                    //gauge.OutputChange -= gaugeHandler;
                }
            };
            onFb.OutputChange += offHandler;

            PowerDownModal.PresentModalDialog(2, "Power off", "Power", message, "Cancel", "Power off now", true, true,
                but =>
                {
                    if (but != 2) // any button except for End cancels
                        timer.Cancel();
                    else
                        timer.Finish();
                });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasStarted(object sender, EventArgs e)
        {
            Debug.Console(1, this, "ShutdownPromptTimer_HasStarted, ShutdownType {0}", _CurrentRoom.ShutdownType);
            // Do we need to check where the UI is? No?
            //var timer = _CurrentRoom.ShutdownPromptTimer;
            //EndMeetingButtonSig.BoolValue = true;
            //ShareButtonSig.BoolValue = false;

            Debug.Console(1, this, "ShutdownType {0}", _CurrentRoom.ShutdownType);
            if (_CurrentRoom.ShutdownType == eShutdownType.Manual || _CurrentRoom.ShutdownType == eShutdownType.Vacancy)
                CreateModalDialog();
            else
                Debug.Console(1, this, "not Creating ModalDialog: {0}", _CurrentRoom.ShutdownType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasFinished(object sender, EventArgs e)
        {
            Debug.Console(1, this, "ShutdownPromptTimer_HasFinished");
            PowerDownModal.HideDialog();  
            //EndMeetingButtonSig.BoolValue = false;
            _CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange -= ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            _CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;

            //var timer = _CurrentRoom.;
            //var message = string.Format("System is powering off.\n{0} seconds remaining", _CurrentRoom.ShutdownPromptSeconds);
            //PowerDownModal.PresentModalDialog(0, "Power off", "Power", message, "Cancel", "", true, true,
            //    but =>
            //    {
            //        if (but != 2) // any button except for End cancels
            //            timer.Cancel();
            //        else
            //            timer.Finish();
            //    });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_WasCancelled(object sender, EventArgs e)
        {
            Debug.Console(1, this, "ShutdownPromptTimer_WasCancelled");
            if (PowerDownModal != null)
                PowerDownModal.HideDialog();
            EndMeetingButtonSig.BoolValue = false;
            //ShareButtonSig.BoolValue = CurrentRoom.OnFeedback.BoolValue;

            _CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange += ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            _CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;
        }

        void ShutdownPromptTimer_TimeRemainingFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(1, this, "ShutdownPromptTimer_TimeRemainingFeedback_OutputChange {0}", (sender as StringFeedback).StringValue);
            var message = string.Format("System will power off in {0} seconds", (sender as StringFeedback).StringValue);
            TriList.StringInput[ModalDialog.MessageTextJoin].StringValue = message;
        }

        void ShutdownPromptTimer_PercentFeedback_OutputChange(object sender, EventArgs e)
        {
            var value = (ushort)((sender as IntFeedback).UShortValue * 65535 / 100);
            Debug.Console(1, this, "ShutdownPromptTimer_PercentFeedback_OutputChange {0}", value);
            TriList.UShortInput[ModalDialog.TimerGaugeJoin].UShortValue = value;
        }

        /// <summary>
        /// 
        /// </summary>
        void CurrentRoom_IsWarmingFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(1, this, "CurrentRoom_IsWarmingFeedback_OutputChange {0}", _CurrentRoom.IsWarmingUpFeedback.BoolValue);
            if (_CurrentRoom.IsWarmingUpFeedback.BoolValue)
            {
                ShowNotificationRibbon("Room is powering on. Please wait...", 0);
            }
            else
            {
                ShowNotificationRibbon("Room is powered on. Welcome.", 2000);
            }
        }

        void IsCoolingDownFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(1, this, "IsCoolingDownFeedback_OutputChange {0}", _CurrentRoom.IsCoolingDownFeedback.BoolValue);
            if (_CurrentRoom.IsCoolingDownFeedback.BoolValue)
            {
                ShowNotificationRibbon("Room is powering off. Please wait.", 0);
            }
            else
            {
                HideNotificationRibbon();
            }
        }

        /// <summary>
        /// For room on/off changes
        /// </summary>
        void CurrentRoom_OnFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(1, this, "CurrentRoom_OnFeedback_OutputChange");
            CurrentRoom_SyncOnFeedback();
        }

        void CurrentRoom_SyncOnFeedback()
        {
            try
            {
                var value = _CurrentRoom.OnFeedback.BoolValue;
                Debug.Console(2, this, "CurrentRoom_SyncOnFeedback, Is on event={0}", value);
                TriList.BooleanInput[UIBoolJoin.RoomIsOn].BoolValue = value;

                if (value) //ON
                {
                    //SetupActivityFooterWhenRoomOn();
                    //TriList.BooleanInput[UIBoolJoin.SelectASourceVisible].BoolValue = false;
                    //TriList.BooleanInput[UIBoolJoin.SourceStagingBarVisible].BoolValue = true;
                    TriList.BooleanInput[StartPageVisibleJoin].BoolValue = false;

                }
                else
                {
                    //SetupActivityFooterWhenRoomOff();
                    //ShowLogo();
                    TriList.BooleanInput[StartPageVisibleJoin].BoolValue = true;
                    //TriList.BooleanInput[UIBoolJoin.SourceStagingBarVisible].BoolValue = false;
                    //TriList.BooleanInput[UIBoolJoin.SelectASourceVisible].BoolValue = false;
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "CurrentRoom_SyncOnFeedback ERROR: {0}", e.Message);
            }
 
        }

        /// <summary>
        /// 
        /// </summary>
        //void CancelPowerOffTimer()
        //{
        //    Debug.Console(1, this, "CancelPowerOffTimer", classname);
        //    if (PowerOffTimer != null)
        //    {
        //        PowerOffTimer.Stop();
        //        PowerOffTimer = null;
        //    }
        //}

        /// <summary>
        /// Reveals a message on the notification ribbon until cleared
        /// </summary>
        /// <param name="message">Text to display</param>
        /// <param name="timeout">Time in ms to display. 0 to keep on screen</param>
        public void ShowNotificationRibbon(string message, int timeout)
        {
            Debug.Console(1, this, "ShowNotificationRibbon");
            TriList.SetString(UIStringJoin.NotificationRibbonText, message);
            TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, true);
            if (timeout > 0)
            {
                if (RibbonTimer != null)
                    RibbonTimer.Stop();
                RibbonTimer = new CTimer(o =>
                {
                    TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, false);
                    RibbonTimer = null;
                }, timeout);
            }
        }

        /// <summary>
        /// Hides the notification ribbon
        /// </summary>
        public void HideNotificationRibbon()
        {
            Debug.Console(1, this, "HideNotificationRibbon");
            TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, false);
            if (RibbonTimer != null)
            {
                RibbonTimer.Stop();
                RibbonTimer = null;
            }
        }
    }
}