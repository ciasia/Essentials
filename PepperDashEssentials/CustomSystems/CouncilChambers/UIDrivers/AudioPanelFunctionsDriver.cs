using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using CI.Essentials.CouncilChambers;
using CI.Essentials.Levels;

namespace CI.Essentials.Audio
{
    public class AudioPanelFunctionsDriver : PanelDriverBase
    {
        private IEssentialsAudioRoom _currentRoom;

        private SmartObject VolumeSO;
        public SubpageReferenceList VolumeSrl { get; set; }
        private uint _volumeListCount;

        string classname = "AudioUIDriver";

        public AudioPanelFunctionsDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            VolumeSO = TriList.SmartObjects[AudioPanelFunctionsJoins.faderSrl];
            Debug.Console(2, "{0}, VolumeSO {1}= null", classname, VolumeSO == null ? "=" : "!");
            VolumeSrl = new SubpageReferenceList(TriList, AudioPanelFunctionsJoins.faderSrl, 2, 1, 1);
            Debug.Console(2, "{0}, VolumeSrl {0}= null", classname, VolumeSrl == null ? "=" : "!");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, "{0}, Show", classname);

            //ShowVolumeGauge = true;

            // Attach actions
            //TriList.SetSigFalseAction(UIBoolJoin.VolumeButtonPopupPress, VolumeButtonsTogglePress);

            // Volume related things
            TriList.SetSigFalseAction(UIBoolJoin.VolumeDefaultPress, () => _currentRoom.SetDefaultLevels());
            TriList.SetString(UIStringJoin.AdvancedVolumeSlider1Text, "Room Volume");
        }


        #region volume

        /// <summary>
        /// Called from button presses on level, where We can assume we want
        /// to change to the proper level.
        /// </summary>
        /// <param name="key">The key name of the route to run</param>
        void SetVolume(string key, ushort value)
        {
            Debug.Console(1, "{0}, UiLevelSet {1} {2}", classname, key, value);
            var dev_ = _currentRoom.audio.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.SetVolume(value);
            else
                Debug.Console(1, "{0}, CurrentVolumeControls == null", classname);
        }

        void MuteToggle(string key)
        {
            Debug.Console(1, "{0}, UiLevelMuteToggle {1}", key);
            var dev_ = _currentRoom.audio.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.MuteToggle();
            else
                Debug.Console(1, "{0}, CurrentVolumeControls == null", classname);
        }

        void RefreshVolumeList()
        {
            try
            {
                var config = _currentRoom.audio.config.VolumeList;
                var volList = config.OrderBy(kv => kv.Value.Order);

                // Setup sources list			
                VolumeSrl.Clear();
                uint i = 1; // counter for UI list
                foreach (var kvp in volList)
                {
                    var volConfig = kvp.Value;
                    var key_ = volConfig.LevelKey;
                    Debug.Console(1, "{0}, RefreshVolumeList {1}, {2} {3}", classname, key_, volConfig.Label, volConfig.IncludeInVolumeList);
                    Debug.Console(2, "{0}, RefreshVolumeList, VolumeSrl {1}= null", classname, VolumeSrl == null ? "=" : "!");
                    // Skip sources marked as not included, and filter list of non-sharable sources when in call
                    // or on share screen
                    if (!volConfig.IncludeInVolumeList)
                    {
                        Debug.Console(1, "{0}, Skipping {1}", classname, volConfig.Label);
                        continue;
                    }
                    if (_currentRoom.audio.VolumeControlList.ContainsKey(key_))
                    {
                        var dev_ = _currentRoom.audio.VolumeControlList[key_].CurrentControl;
                        Debug.Console(2, "{0}, RefreshVolumeList - VolumeControlList[{1}].CurrentControl {2}= null", classname, key_, dev_ == null ? "=" : "!");
                        if (dev_ != null) // connect buttons
                        {
                            var fbDev_ = dev_ as IBasicVolumeWithFeedback;
                            //Debug.Console(2, "{0}, RefreshVolumeList, {1} fbDev_ {2}= null", classname, key_, fbDev_ == null ? "=" : "!");
                            var level_ = new SubpageReferenceListLevelItem(i, VolumeSrl, volConfig,
                                  u => { fbDev_.SetVolume(u); },
                                  b => { if (!b) fbDev_.MuteToggle(); }
                                  );
                            //Debug.Console(2, "{0}, RefreshVolumeList, {1} level_ {2}= null", classname, key_, level_ == null ? "=" : "!");
                            VolumeSrl.AddItem(level_); // add to the SRL
                            //Debug.Console(2, "{0}, RefreshVolumeList, {1} VolumeSrl added level", classname, key_);
                            level_.RegisterForLevelChange(_currentRoom.audio);
                            //Debug.Console(2, "{0}, RefreshVolumeList, {0} RegisterForLevelChange", classname, key_);
                            string joinKey_ = String.Format("Item {0} Visible", i);
                            //Debug.Console(2, "{0}, RefreshVolumeList Setting SmartObject {1}", classname, joinKey_);
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
                            Debug.Console(1, "{0}, CurrentVolumeControls {1} == null", classname, key_);
                    }
                    else
                        Debug.Console(1, "{0}, VolumeControlList.ContainsKey({1}) == false", classname, key_);
                    i++;
                }
                _volumeListCount = (i - 1);
                VolumeSrl.Count = (ushort)_volumeListCount;
            }
            catch (Exception e)
            {
                Debug.Console(1, "{0}, RefreshVolumeList ERROR: {1}", classname, e.Message);
            }
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsAudioRoom room)
        {
            Debug.Console(1, "{0}, DisconnectCurrentRoom", classname);
            _currentRoom = room;

            if (_currentRoom != null)
            {
                // Disconnect current room
                room.audio.MasterVolumeDeviceChange -= this.CurrentRoom_CurrentMasterAudioDeviceChange;
                room.audio.VolumeDeviceListChange -= this.CurrentRoom_CurrentAudioDeviceListChanged;
                ClearAudioDeviceConnections();
            }
            //else
            //    Debug.Console(1, "{0}, CurrentRoom == null", classname);
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void ClearAudioDeviceConnections()
        {
            Debug.Console(1, "{0}, ClearAudioDeviceConnections", classname);

            TriList.ClearBoolSigAction(UIBoolJoin.VolumeUpPress);
            TriList.ClearBoolSigAction(UIBoolJoin.VolumeDownPress);
            TriList.ClearBoolSigAction(UIBoolJoin.Volume1ProgramMutePressAndFB);

            var fDev = _currentRoom.audio.MasterVolumeControl as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                TriList.ClearUShortSigAction(UIUshortJoin.VolumeSlider1Value);
                fDev.VolumeLevelFeedback.UnlinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
            }
            VolumeSrl.Clear();
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsAudioRoom room)
        {
            Debug.Console(1, "{0}, ConnectCurrentRoom", classname);
            _currentRoom = room;
            //Debug.Console(1, "{0}, _CurrentRoom {1}= null", classname, _currentRoom == null ? "=" : "!");

            if (_currentRoom != null)
            {
                Debug.Console(1, "{0}, subscribing to CurrentVolumeDeviceChange", classname);
                room.audio.MasterVolumeDeviceChange += CurrentRoom_CurrentMasterAudioDeviceChange;
                room.audio.VolumeDeviceListChange += CurrentRoom_CurrentAudioDeviceListChanged;
                RefreshAudioDeviceConnections();
            } 

        }
    
        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void RefreshAudioDeviceConnections()
        {
            Debug.Console(1, "{0}, RefreshAudioDeviceConnections", classname);
            if (_currentRoom != null)
            {
                var dev = _currentRoom.audio.MasterVolumeControl.CurrentControl;
                if (dev != null) // connect buttons
                {
                    Debug.Console(1, "{0}, CurrentVolumeControls connect buttons", classname);
                    TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, VolumeUpPress);
                    TriList.SetBoolSigAction(UIBoolJoin.VolumeDownPress, VolumeDownPress);
                    TriList.SetSigFalseAction(UIBoolJoin.Volume1ProgramMutePressAndFB, dev.MuteToggle);
                }
                else
                    Debug.Console(1, "{0}, CurrentVolumeControls == null", classname);

                var fbDev = dev as IBasicVolumeWithFeedback;
                if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value].UShortValue = 0;
                else
                {
                    Debug.Console(1, "{0}, connect feedbacks", classname);
                    // slider
                    TriList.SetUShortSigAction(UIUshortJoin.VolumeSlider1Value, fbDev.SetVolume);
                    // feedbacks
                    fbDev.MuteFeedback.LinkInputSig(TriList.BooleanInput[UIBoolJoin.Volume1ProgramMutePressAndFB]);
                    fbDev.VolumeLevelFeedback.LinkInputSig(
                        TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
                }

                RefreshVolumeList();
            }
        }

        /// <summary>
        /// Handler for when the room's volume control device changes
        /// </summary>
        void CurrentRoom_CurrentMasterAudioDeviceChange(object sender, VolumeDeviceChangeEventArgs args)
        {
            Debug.Console(1, "{0}, CurrentRoom_CurrentAudioDeviceChange", classname);
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
            Debug.Console(1, "{0}, CurrentRoom_CurrentAudioDeviceListChanged {1}", classname, args.NewDev.ToString());
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
            Debug.Console(1, "AudioPanelFunctionsDriver - VolumeButtonsTogglePress");
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
            Debug.Console(1, "{0}, VolumeUpPress {1}", classname, state);
            // extend timeouts
            //if (ShowVolumeGauge)
            VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            var dev_ = _currentRoom.audio.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeUp(state);
            else
                Debug.Console(1, "{0}, CurrentVolumeControls == null", classname);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, "{0}, VolumeDownPress {1}", classname, state);
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            var dev_ = _currentRoom.audio.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeDown(state);
            else
                Debug.Console(1, "{0}, CurrentVolumeControls == null", classname);
        }

        #endregion

    }
}