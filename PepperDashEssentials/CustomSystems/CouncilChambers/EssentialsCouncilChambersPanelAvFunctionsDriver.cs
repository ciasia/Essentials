using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace PepperDashEssentials.CustomSystems
{
    public class EssentialsCouncilChambersPanelAvFunctionsDriver : PanelDriverBase
    {
        CrestronTouchpanelPropertiesConfig Config;

        /// <summary>
        /// no defaultDisplayKey
        /// </summary>
        EssentialsAvRoomPropertiesConfig PropertiesConfig { get; set; }


        public uint StartPageVisibleJoin { get; private set; }

        /// <summary>
        /// Whether volume ramping from this panel will show the volume
        /// gauge popup.
        /// </summary>
        public bool ShowVolumeGauge { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultRoomKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEssentialsAVRoom CurrentRoom
        {
            get { return _CurrentRoom; }
            set
            {
                SetCurrentRoom(value);
            }
        }
        IEssentialsAVRoom _CurrentRoom;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        public PanelDriverBase Parent { get; private set; }

        public EssentialsCouncilChambersPanelAvFunctionsDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Config = config;
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, "PanelAVFunctionsDriver, Show()");
 
            if (CurrentRoom == null)
            {
                Debug.Console(1, "ERROR: AVUIFunctionsDriver, Cannot show. No room assigned");
                return;
            }

            //var roomConf = CurrentRoom.PropertiesConfig;
            var roomConf = PropertiesConfig;
 
            TriList.SetBool(UIBoolJoin.DateAndTimeVisible, Config.ShowDate && Config.ShowTime);

            TriList.SetSigFalseAction(UIBoolJoinCouncilChambers.PowerPress, PowerButtonPressed);
        }

        void SetCurrentRoom(IEssentialsAVRoom room)
        {
            if (_CurrentRoom == room) return;
            // Disconnect current (probably never called)

            if (_CurrentRoom != null)
                _CurrentRoom.ConfigChanged -= room_ConfigChanged;

            room.ConfigChanged -= room_ConfigChanged;
            room.ConfigChanged += room_ConfigChanged;

            //if (room.IsMobileControlEnabled)
            //{
            //    StartPageVisibleJoin = UIBoolJoin.StartMCPageVisible;
            //    //UpdateMCJoins(room);

            //    if (_CurrentRoom != null)
            //        _CurrentRoom.MobileControlRoomBridge.UserCodeChanged -= MobileControlRoomBridge_UserCodeChanged;

            //    room.MobileControlRoomBridge.UserCodeChanged -= MobileControlRoomBridge_UserCodeChanged;
            //    room.MobileControlRoomBridge.UserCodeChanged += MobileControlRoomBridge_UserCodeChanged;
            //}
            //else
            //{
                StartPageVisibleJoin = UIBoolJoin.StartPageVisible;
            //}

            RefreshCurrentRoom(room);
        }

        /// <summary>
        /// Fires when room config of current room has changed.  Meant to refresh room values to propegate any updates to UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void room_ConfigChanged(object sender, EventArgs e)
        {
            RefreshCurrentRoom(_CurrentRoom);
        }

        /// <summary>
        /// Helper for property setter. Sets the panel to the given room, latching up all functionality
        /// </summary>
        void RefreshCurrentRoom(IEssentialsAVRoom room)
        {

            if (_CurrentRoom != null)
            {
                // Disconnect current room
                //_CurrentRoom.CurrentVolumeDeviceChange -= this.CurrentRoom_CurrentAudioDeviceChange;
                //ClearAudioDeviceConnections();
                //_CurrentRoom.CurrentSourceChange -= this.CurrentRoom_SourceInfoChange;
                //DisconnectSource(_CurrentRoom.CurrentSourceInfo);
                _CurrentRoom.ShutdownPromptTimer.HasStarted -= ShutdownPromptTimer_HasStarted;
                _CurrentRoom.ShutdownPromptTimer.HasFinished -= ShutdownPromptTimer_HasFinished;
                _CurrentRoom.ShutdownPromptTimer.WasCancelled -= ShutdownPromptTimer_WasCancelled;

                //_CurrentRoom.OnFeedback.OutputChange -= CurrentRoom_OnFeedback_OutputChange;
                _CurrentRoom.IsWarmingUpFeedback.OutputChange -= CurrentRoom_IsWarmingFeedback_OutputChange;
                _CurrentRoom.IsCoolingDownFeedback.OutputChange -= CurrentRoom_IsCoolingDownFeedback_OutputChange;
            }

            _CurrentRoom = room;

            if (_CurrentRoom != null)
            {
                // get the source list config and set up the source list

                //SetupSourceList();

                // Name and logo
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = _CurrentRoom.Name;
                //ShowLogo();

                // Shutdown timer
                _CurrentRoom.ShutdownPromptTimer.HasStarted += ShutdownPromptTimer_HasStarted;
                _CurrentRoom.ShutdownPromptTimer.HasFinished += ShutdownPromptTimer_HasFinished;
                _CurrentRoom.ShutdownPromptTimer.WasCancelled += ShutdownPromptTimer_WasCancelled;

                // Link up all the change events from the room
                //_CurrentRoom.OnFeedback.OutputChange += CurrentRoom_OnFeedback_OutputChange;
                //CurrentRoom_SyncOnFeedback();
                _CurrentRoom.IsWarmingUpFeedback.OutputChange += CurrentRoom_IsWarmingFeedback_OutputChange;
                _CurrentRoom.IsCoolingDownFeedback.OutputChange += CurrentRoom_IsCoolingDownFeedback_OutputChange;
                //_CurrentRoom.InCallFeedback.OutputChange += CurrentRoom_InCallFeedback_OutputChange;


                //_CurrentRoom.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                //RefreshAudioDeviceConnections();
                //_CurrentRoom.CurrentSourceChange += CurrentRoom_SourceInfoChange;
                //RefreshSourceInfo();


                //if (_CurrentRoom != null)
                //    _CurrentRoom.CurrentSourceChange += new SourceInfoChangeHandler(CurrentRoom_CurrentSingleSourceChange);

                // Moved to EssentialsVideoCodecUiDriver
                //TriList.SetSigFalseAction(UIBoolJoin.CallStopSharingPress, () => _CurrentRoom.RunRouteAction("codecOsd", _CurrentRoom.SourceListKey));

                //(Parent as EssentialsPanelMainInterfaceDriver).HeaderDriver.SetupHeaderButtons(this, CurrentRoom);
            }
            else
            {
                // Clear sigs that need to be
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = "Select a room";
            }
        }

        #region Power

        void PowerButtonPressed()
        {
            if (!CurrentRoom.OnFeedback.BoolValue
                || CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue)
                return;

            CurrentRoom.StartShutdown(eShutdownType.Manual);
        }

        /// <summary>
        /// 
        /// </summary>
        public uint PowerOffTimeout { get; set; }

        /// <summary>
        /// Will auto-timeout a power off
        /// </summary>
        CTimer PowerOffTimer;

        /// <summary>
        /// 
        /// </summary>
        ModalDialog PowerDownModal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasStarted(object sender, EventArgs e)
        {
            // Do we need to check where the UI is? No?
            var timer = CurrentRoom.ShutdownPromptTimer;
            SetActivityFooterFeedbacks();

            if (CurrentRoom.ShutdownType == eShutdownType.Manual || CurrentRoom.ShutdownType == eShutdownType.Vacancy)
            {
                PowerDownModal = new ModalDialog(TriList);
                var message = string.Format("System will power down in {0} seconds", CurrentRoom.ShutdownPromptSeconds);

                // Attach timer things to modal
                CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange += ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
                CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange += ShutdownPromptTimer_PercentFeedback_OutputChange;

                // respond to offs by cancelling dialog
                var onFb = CurrentRoom.OnFeedback;
                EventHandler<FeedbackEventArgs> offHandler = null;
                offHandler = (o, a) =>
                {
                    if (!onFb.BoolValue)
                    {
                        PowerDownModal.HideDialog();
                        SetActivityFooterFeedbacks();
                        onFb.OutputChange -= offHandler;
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasFinished(object sender, EventArgs e)
        {
            SetActivityFooterFeedbacks();
            CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange -= ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_WasCancelled(object sender, EventArgs e)
        {
            if (PowerDownModal != null)
                PowerDownModal.HideDialog();
            SetActivityFooterFeedbacks();

            CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange += ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;
        }

        /// <summary>
        /// Event handler for countdown timer on power off modal
        /// </summary>
        void ShutdownPromptTimer_TimeRemainingFeedback_OutputChange(object sender, EventArgs e)
        {

            var message = string.Format("Meeting will end in {0} seconds", (sender as StringFeedback).StringValue);
            TriList.StringInput[ModalDialog.MessageTextJoin].StringValue = message;
        }

        /// <summary>
        /// Event handler for percentage on power off countdown
        /// </summary>
        void ShutdownPromptTimer_PercentFeedback_OutputChange(object sender, EventArgs e)
        {
            var value = (ushort)((sender as IntFeedback).UShortValue * 65535 / 100);
            TriList.UShortInput[ModalDialog.TimerGaugeJoin].UShortValue = value;
        }

        /// <summary>
        /// 
        /// </summary>
        void CancelPowerOffTimer()
        {
            if (PowerOffTimer != null)
            {
                PowerOffTimer.Stop();
                PowerOffTimer = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CurrentRoom_IsWarmingFeedback_OutputChange(object sender, EventArgs e)
        {
            if (CurrentRoom.IsWarmingUpFeedback.BoolValue)
            {
                ShowNotificationRibbon("Room is powering on. Please wait...", 0);
            }
            else
            {
                ShowNotificationRibbon("Room is powered on. Welcome.", 2000);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentRoom_IsCoolingDownFeedback_OutputChange(object sender, EventArgs e)
        {
            if (CurrentRoom.IsCoolingDownFeedback.BoolValue)
            {
                ShowNotificationRibbon("Room is powering off. Please wait.", 0);
            }
            else
            {
                HideNotificationRibbon();
            }
        }

        /// <summary>
        /// Controls timeout of notification ribbon timer
        /// </summary>
        CTimer RibbonTimer;

        /// <summary>
        /// Reveals a message on the notification ribbon until cleared
        /// </summary>
        /// <param name="message">Text to display</param>
        /// <param name="timeout">Time in ms to display. 0 to keep on screen</param>
        public void ShowNotificationRibbon(string message, int timeout)
        {
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
            TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, false);
            if (RibbonTimer != null)
            {
                RibbonTimer.Stop();
                RibbonTimer = null;
            }
        }
        #endregion


        /// <summary>
        /// Single point call for setting the feedbacks on the activity buttons
        /// </summary>
        void SetActivityFooterFeedbacks()
        {
            if (CurrentRoom != null)
            {
            }
       }

    }
}