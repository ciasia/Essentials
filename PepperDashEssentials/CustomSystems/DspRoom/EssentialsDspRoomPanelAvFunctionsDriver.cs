using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.DspRoom
{
    public class EssentialsDspRoomPanelAvFunctionsDriver : PanelDriverBase, IAVDriver
    {
        /// <summary>
        /// The parent driver for this
        /// </summary>
        public PanelDriverBase Parent { get; private set; }

        CrestronTouchpanelPropertiesConfig Config;

        /// <summary>
        /// 
        /// </summary>
        public string DefaultRoomKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEssentialsDspRoom CurrentRoom
        {
            get { return _CurrentRoom; }
            set
            {
                SetCurrentRoom(value);
            }
        }
        IEssentialsDspRoom _CurrentRoom;

        public EssentialsDspRoomPanelAvFunctionsDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Config = config;
            Parent = parent;
            Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            Debug.Console(1, "Loading EssentialsDspRoomPanelAvFunctionsDriver");

            //ShowVolumeGauge = true;

            // One-second pulse extender for volume gauge
            //VolumeGaugeFeedback = new BoolFeedbackPulseExtender(1500);
            //VolumeGaugeFeedback.Feedback
            //    .LinkInputSig(TriList.BooleanInput[UIBoolJoin.VolumeGaugePopupVisible]);

            //VolumeButtonsPopupFeedback = new BoolFeedbackPulseExtender(4000);
            //VolumeButtonsPopupFeedback.Feedback
            //    .LinkInputSig(TriList.BooleanInput[UIBoolJoin.VolumeButtonPopupVisible]);
        }

		/// <summary>
		/// 
		/// </summary>
        public override void Show()
        {
            Debug.Console(1, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - Show");
            if (CurrentRoom == null)
            {
                Debug.Console(1, "ERROR: DspRoom AVUIFunctionsDriver, Cannot show. No room assigned");
                return;
            }

            var roomConf = CurrentRoom.PropertiesConfig;

            ShowVolumeGauge = true;

            // Attach actions
            //TriList.SetSigFalseAction(UIBoolJoin.VolumeButtonPopupPress, VolumeButtonsTogglePress);

            // Volume related things
            TriList.SetSigFalseAction(UIBoolJoin.VolumeDefaultPress, () => CurrentRoom.SetDefaultLevels());
            TriList.SetString(UIStringJoin.AdvancedVolumeSlider1Text, "Room Volume");

            base.Show();
        }

        #region room config

        /// <summary>
        /// Fires when room config of current room has changed.  Meant to refresh room values to propegate any updates to UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void room_ConfigChanged(object sender, EventArgs e)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - room_ConfigChanged");
            RefreshCurrentRoom(_CurrentRoom);
        }

        /// <summary>
        /// Helper for property setter. Sets the panel to the given room, latching up all functionality
        /// </summary>
        public void RefreshCurrentRoom(IEssentialsDspRoom room)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - RefreshCurrentRoom");
            if (_CurrentRoom != null)
            {
                // Disconnect current room
                _CurrentRoom.CurrentVolumeDeviceChange -= this.CurrentRoom_CurrentAudioDeviceChange;
                ClearAudioDeviceConnections();
            }
            //else
            //    Debug.Console(1, "DspRoom AVUIFunctionsDriver - _CurrentRoom == null");

            _CurrentRoom = room;

            if (_CurrentRoom != null)
            {
                // Name and logo
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = _CurrentRoom.Name;

                Debug.Console(1, "DspRoom AVUIFunctionsDriver - subscribing to CurrentVolumeDeviceChange");
                _CurrentRoom.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                RefreshAudioDeviceConnections();
            }
            else
            {
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - subscribing to no room selected");

                // Clear sigs that need to be
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = "Select a room";
            }
        }

        void SetCurrentRoom(IEssentialsDspRoom room)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - SetCurrentRoom, _CurrentRoom {0}= null", _CurrentRoom == null ? "=": "!");
            if (_CurrentRoom == room) return;
            // Disconnect current (probably never called)

            if (_CurrentRoom != null)
                _CurrentRoom.ConfigChanged -= room_ConfigChanged;

            room.ConfigChanged -= room_ConfigChanged;
            room.ConfigChanged += room_ConfigChanged;

            RefreshCurrentRoom(room);
        }

        #endregion

        #region volume

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        void RefreshAudioDeviceConnections()
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - RefreshAudioDeviceConnections");
            var dev = CurrentRoom.CurrentVolumeControls;
            if (dev != null) // connect buttons
            {
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls connect buttons");
                TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, VolumeUpPress);
                //TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, b =>
                //    {
                //        Debug.Console(1, "DspRoom AVUIFunctionsDriver - Action aVolumeUpPress {0}", b);
                //        VolumeUpPress(b);
                //    });
                TriList.SetBoolSigAction(UIBoolJoin.VolumeDownPress, VolumeDownPress);
                TriList.SetSigFalseAction(UIBoolJoin.Volume1ProgramMutePressAndFB, dev.MuteToggle);
            }
            else
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls == null");

            var fbDev = dev as IBasicVolumeWithFeedback;
            if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                TriList.UShortInput[UIUshortJoin.VolumeSlider1Value].UShortValue = 0;
            else
            {
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - connect feedbacks");
                // slider
                TriList.SetUShortSigAction(UIUshortJoin.VolumeSlider1Value, fbDev.SetVolume);
                // feedbacks
                fbDev.MuteFeedback.LinkInputSig(TriList.BooleanInput[UIBoolJoin.Volume1ProgramMutePressAndFB]);
                fbDev.VolumeLevelFeedback.LinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
            }
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        void ClearAudioDeviceConnections()
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - ClearAudioDeviceConnections");
            TriList.ClearBoolSigAction(UIBoolJoin.VolumeUpPress);
            TriList.ClearBoolSigAction(UIBoolJoin.VolumeDownPress);
            TriList.ClearBoolSigAction(UIBoolJoin.Volume1ProgramMutePressAndFB);

            var fDev = CurrentRoom.CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                TriList.ClearUShortSigAction(UIUshortJoin.VolumeSlider1Value);
                fDev.VolumeLevelFeedback.UnlinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
            }
        }

        /// <summary>
        /// Handler for when the room's volume control device changes
        /// </summary>
        void CurrentRoom_CurrentAudioDeviceChange(object sender, VolumeDeviceChangeEventArgs args)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentRoom_CurrentAudioDeviceChange");
            if (args.Type == ChangeType.WillChange)
                ClearAudioDeviceConnections();
            else // did change
                RefreshAudioDeviceConnections();
        }

        /// <summary>
        /// Whether volume ramping from this panel will show the volume
        /// gauge popup.
        /// </summary>
        public bool ShowVolumeGauge { get; set; }

        /// <summary>
        /// Controls the extended period that the volume gauge shows on-screen,
        /// as triggered by Volume up/down operations
        /// </summary>
        BoolFeedbackPulseExtender VolumeGaugeFeedback;

        /// <summary>
        /// 
        /// </summary>
        void VolumeButtonsTogglePress()
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - VolumeButtonsTogglePress");
            //if (VolumeButtonsPopupFeedback.BoolValue)
            //    VolumeButtonsPopupFeedback.ClearNow();
            //else
            //{
            //    // Trigger the popup
            //    VolumeButtonsPopupFeedback.BoolValue = true;
            //    VolumeButtonsPopupFeedback.BoolValue = false;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void VolumeUpPress(bool state)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - VolumeUpPress {0}", state);
            // extend timeouts
            //if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            if (CurrentRoom.CurrentVolumeControls != null)
                CurrentRoom.CurrentVolumeControls.VolumeUp(state);
            else
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls == null");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - VolumeDownPress {0}", state);
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            if (CurrentRoom.CurrentVolumeControls != null)
                CurrentRoom.CurrentVolumeControls.VolumeDown(state);
            else
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls == null");
        }

        #endregion

        #region IAVDriver Members


        public JoinedSigInterlock PopupInterlock { get; private set; }

        public void ShowNotificationRibbon(string message, int timeout)
        {
            //throw new NotImplementedException();
        }

        public void HideNotificationRibbon()
        {
            //throw new NotImplementedException();
        }

        public void ShowTech()
        {
            //throw new NotImplementedException();
        }

        public uint StartPageVisibleJoin { get; private set; }

        #endregion
    }
}