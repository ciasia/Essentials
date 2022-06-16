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

        private SmartObject VolumeSO;
        public SubpageReferenceList VolumeSrl { get; set; }
        private uint _volumeListCount;

        public string DefaultRoomKey { get; set; }

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

            VolumeSO = TriList.SmartObjects[EssentialsDspRoomJoins.faderSrl];
            Debug.Console(2, "DspRoom AVUIFunctionsDriver, VolumeSO {0}= null", VolumeSO == null ? "=" : "!");
            VolumeSrl = new SubpageReferenceList(TriList, EssentialsDspRoomJoins.faderSrl, 2, 1, 1);
            Debug.Console(2, "DspRoom AVUIFunctionsDriver, VolumeSrl {0}= null", VolumeSrl == null ? "=" : "!");
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
                _CurrentRoom.MasterVolumeDeviceChange -= this.CurrentRoom_CurrentMasterAudioDeviceChange;
                _CurrentRoom.VolumeDeviceListChange -= this.CurrentRoom_CurrentAudioDeviceListChanged;
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
                _CurrentRoom.MasterVolumeDeviceChange += CurrentRoom_CurrentMasterAudioDeviceChange;
                _CurrentRoom.VolumeDeviceListChange += CurrentRoom_CurrentAudioDeviceListChanged;
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
        /// Called from button presses on level, where We can assume we want
        /// to change to the proper level.
        /// </summary>
        /// <param name="key">The key name of the route to run</param>
        void SetVolume(string key, ushort value)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - UiLevelSet {0} {1}", key, value);
            var dev_ = CurrentRoom.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.SetVolume(value);
            else
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls == null");
        }

        void MuteToggle(string key)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - UiLevelMuteToggle {0}", key);
            var dev_ = CurrentRoom.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.MuteToggle();
            else
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls == null");
        }

        void RefreshVolumeList()
        {
            try
            {
                var config = CurrentRoom.PropertiesConfig.VolumeList;
                var volList = config.OrderBy(kv => kv.Value.Order);

                // Setup sources list			
                VolumeSrl.Clear();
                uint i = 1; // counter for UI list
                foreach (var kvp in volList)
                {
                    var volConfig = kvp.Value;
                    var key_ = volConfig.LevelKey;
                    Debug.Console(1, "$$$$ RefreshVolumeList {0}, {1} {2}", key_, volConfig.Label, volConfig.IncludeInVolumeList);
                    Debug.Console(2, "DspRoom RefreshVolumeList, VolumeSrl {0}= null", VolumeSrl == null ? "=" : "!");
				    // Skip sources marked as not included, and filter list of non-sharable sources when in call
				    // or on share screen
                    if (!volConfig.IncludeInVolumeList) 
				    {
                        Debug.Console(1, "Skipping {0}", volConfig.Label);
					    continue;
				    }
                    if (CurrentRoom.VolumeControlList.ContainsKey(key_))
                    {
                        var dev_ = CurrentRoom.VolumeControlList[key_].CurrentControl;
                        Debug.Console(2, "DspRoom RefreshVolumeList - VolumeControlList[{0}].CurrentControl {1}= null", key_, dev_ == null ? "=" : "!");
                        if (dev_ != null) // connect buttons
                        {
                            var fbDev_ = dev_ as IBasicVolumeWithFeedback;
                            //Debug.Console(2, "DspRoom RefreshVolumeList, {0} fbDev_ {1}= null", key_, fbDev_ == null ? "=" : "!");
                            var level_ = new SubpageReferenceListLevelItem(i, VolumeSrl, volConfig,
                                  u => { fbDev_.SetVolume(u); },
                                  b => { if (!b) fbDev_.MuteToggle(); }
                                  );
                            //Debug.Console(2, "DspRoom RefreshVolumeList, {0} level_ {1}= null", key_, level_ == null ? "=" : "!");
                            VolumeSrl.AddItem(level_); // add to the SRL
                            //Debug.Console(2, "DspRoom RefreshVolumeList, {0} VolumeSrl added level", key_);
                            level_.RegisterForLevelChange(_CurrentRoom);
                            //Debug.Console(2, "DspRoom RefreshVolumeList, {0} RegisterForLevelChange", key_);
                            string joinKey_ = String.Format("Item {0} Visible", i);
                            //Debug.Console(2, "DspRoom RefreshVolumeList Setting SmartObject {0}", joinKey_);
                            VolumeSO.BooleanInput[joinKey_].BoolValue = true;

                            if (fbDev_ == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                                VolumeSrl.UShortInputSig(i, 1).UShortValue = (ushort)0;
                            else
                            {
                                // feedbacks
                                fbDev_.MuteFeedback.LinkInputSig(VolumeSrl.BoolInputSig(i, 1));
                                fbDev_.VolumeLevelFeedback.LinkInputSig(VolumeSrl.UShortInputSig(i, 1));
                            }
                        }
                        else
                            Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls {0} == null", key_);
                    }
                    else
                        Debug.Console(1, "DspRoom AVUIFunctionsDriver - VolumeControlList.ContainsKey({0}) == false", key_);
                    i++;
			    }
                _volumeListCount = (i - 1);
                VolumeSrl.Count = (ushort)_volumeListCount;
            }
            catch (Exception e)
            {
                Debug.Console(1, "RefreshVolumeList ERROR: {0}", e.Message);
            }
		}


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        void RefreshAudioDeviceConnections()
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - RefreshAudioDeviceConnections");
            var dev = CurrentRoom.MasterVolumeControl.CurrentControl;
            if (dev != null) // connect buttons
            {
                Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentVolumeControls connect buttons");
                TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, VolumeUpPress);
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

            RefreshVolumeList();
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

            var fDev = CurrentRoom.MasterVolumeControl as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                TriList.ClearUShortSigAction(UIUshortJoin.VolumeSlider1Value);
                fDev.VolumeLevelFeedback.UnlinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
            }
            VolumeSrl.Clear();
        }

        /// <summary>
        /// Handler for when the room's volume control device changes
        /// </summary>
        void CurrentRoom_CurrentMasterAudioDeviceChange(object sender, VolumeDeviceChangeEventArgs args)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentRoom_CurrentAudioDeviceChange");
            if (args.Type == ChangeType.WillChange)
                ClearAudioDeviceConnections();
            else // did change
                RefreshAudioDeviceConnections();
        }

         /// <summary>
        /// Handler for when a room's volume control list device changes
        /// </summary>
        void CurrentRoom_CurrentAudioDeviceListChanged(object sender, VolumeDeviceChangeEventArgs args)
        {
            Debug.Console(1, "DspRoom AVUIFunctionsDriver - CurrentRoom_CurrentAudioDeviceListChanged {0}", args.NewDev.ToString());
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
            var dev_ = CurrentRoom.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeUp(state);
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
            var dev_ = CurrentRoom.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeDown(state);
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