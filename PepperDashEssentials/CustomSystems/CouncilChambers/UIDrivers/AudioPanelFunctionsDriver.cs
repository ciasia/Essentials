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
using CI.Essentials.Utilities;
using CI.Essentials.UI;

namespace CI.Essentials.Audio
{
    public class AudioPanelFunctionsDriver : PanelDriverBase, IEssentialsConnectableRoomDriver, IKeyed
    {
        string IKeyed.Key { get { return "AudioUIDriver"; } }
        //string Key = "AudioUIDriver";
        private IEssentialsAudioRoom _currentRoom;

        private SmartObject VolumeSO;
        public SubpageReferenceList VolumeSrl { get; set; }
        private uint _volumeListCount;

        public AudioPanelFunctionsDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Debug.Console(1, this, "Loading");
            VolumeSO = TriList.SmartObjects[SmartJoins.faderSrl];
            Debug.Console(2, this, "VolumeSO".IsNullString(VolumeSO));
            VolumeSrl = new SubpageReferenceList(TriList, SmartJoins.faderSrl, 4, 1, 1);
            Debug.Console(2, this, "VolumeSrl".IsNullString(VolumeSrl));
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, this, "Show");

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
            Debug.Console(1, this, "UiLevelSet {0} {1}", key, value);
            var dev_ = _currentRoom.audio.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.SetVolume(value);
            else
                Debug.Console(2, this, "CurrentVolumeControls".IsNullString(dev_));
        }

        void MuteToggle(string key)
        {
            Debug.Console(1, this, "UiLevelMuteToggle {1}", key);
            var dev_ = _currentRoom.audio.VolumeControlList[key].CurrentControl as IBasicVolumeWithFeedback;
            if (dev_ != null)
                dev_.MuteToggle();
            else
                Debug.Console(1, this, "CurrentVolumeControls".IsNullString(dev_));
        }

        /// <summary>
        /// Volume SRL - connect the joins to funcs
        /// </summary>
        void RefreshVolumeList()
        {
            try
            {
                Debug.Console(1, this, "RefreshVolumeList");
                var config = _currentRoom.audio.config.VolumeList;
                var volList = config.OrderBy(kv => kv.Value.Order);

                // Setup sources list			
                VolumeSrl.Clear();
                uint i = 1; // counter for UI list
                foreach (var kvp in volList)
                {
                    var volConfig = kvp.Value;
                    var key_ = volConfig.LevelKey;
                    Debug.Console(1, this, "RefreshVolumeList {0}, {1} {2}", key_, volConfig.Label, volConfig.IncludeInVolumeList);
                    Debug.Console(2, this, "VolumeSrl".IsNullString(VolumeSrl));
                    // Skip sources marked as not included, and filter list of non-sharable sources when in call
                    // or on share screen
                    if (!volConfig.IncludeInVolumeList)
                    {
                        Debug.Console(1, this, "Skipping {0}", volConfig.Label);
                        continue;
                    }
                    if (_currentRoom.audio.VolumeControlList.ContainsKey(key_))
                    {
                        var dev_ = _currentRoom.audio.VolumeControlList[key_].CurrentControl;
                        Debug.Console(2, this, "RefreshVolumeList - VolumeControlList[{0}].{1}", key_, "CurrentControl".IsNullString(dev_));
                        if (dev_ != null) // connect buttons
                        {
                            var fbDev_ = dev_ as IBasicVolumeWithFeedback;
                            Debug.Console(2, this, "RefreshVolumeList, {0} {1}", key_, "CurrentControl".IsNullString(fbDev_));
                            var level_ = new SubpageReferenceListLevelItem(i, VolumeSrl, volConfig,
                                  v => { fbDev_.SetVolume(v); },
                                  b => { if (!b) fbDev_.MuteToggle(); },
                                  u => { fbDev_.VolumeUp(u); },
                                  d => { fbDev_.VolumeDown(d); }
                                  );
                            Debug.Console(2, this, "RefreshVolumeList, {0} {1}", key_, "level_".IsNullString(level_));
                            VolumeSrl.AddItem(level_); // add to the SRL
                            Debug.Console(2, this, "RefreshVolumeList, {0} VolumeSrl added level", key_);
                            level_.RegisterForLevelChange(_currentRoom.audio);
                            Debug.Console(2, this, "RefreshVolumeList, {0} RegisterForLevelChange", key_);
                            string visibleKey_ = String.Format("Item {0} Visible", i);
                            //Debug.Console(2, this, "RefreshVolumeList Setting SmartObject {1}", classname, joinKey_);
                            VolumeSO.BooleanInput[visibleKey_].BoolValue = true;

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
                            Debug.Console(1, this, "CurrentVolumeControls".IsNullString(key_));
                    }
                    else
                        Debug.Console(1, this, "VolumeControlList.ContainsKey({0}) == false", key_);
                    i++;
                }
                _volumeListCount = (i - 1);
                VolumeSrl.Count = (ushort)_volumeListCount;
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "RefreshVolumeList ERROR: {0}", e.Message);
            }
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "DisconnectCurrentRoom");
            try
            {
                _currentRoom = (IEssentialsAudioRoom)room;
                //_currentRoom = room as IEssentialsAudioRoom;

                if (_currentRoom != null)
                {
                    // Disconnect current room
                    _currentRoom.audio.MasterVolumeDeviceChange -= this.CurrentRoom_CurrentMasterAudioDeviceChange;
                    _currentRoom.audio.VolumeDeviceListChange -= this.CurrentRoom_CurrentAudioDeviceListChanged;
                    ClearDeviceConnections();
                }
                else
                    Debug.Console(1, this, "_currentRoom".IsNullString(_currentRoom));
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "DisconnectCurrentRoom ERROR: {0}", e.Message);
            }

        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void ClearDeviceConnections()
        {
            Debug.Console(1, this, "ClearDeviceConnections");

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
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            try
            {
               _currentRoom = (IEssentialsAudioRoom)room;
                //Debug.Console(1, this, "_CurrentRoom {1}= null", classname, _currentRoom == null ? "=" : "!");

                if (_currentRoom != null)
                {
                    Debug.Console(1, this, "subscribing to CurrentVolumeDeviceChange");
                    _currentRoom.audio.MasterVolumeDeviceChange += CurrentRoom_CurrentMasterAudioDeviceChange;
                    _currentRoom.audio.VolumeDeviceListChange += CurrentRoom_CurrentAudioDeviceListChanged;
                    RefreshDeviceConnections();
                }
                else
                    Debug.Console(1, this, "_currentRoom".IsNullString(_currentRoom));
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "ConnectCurrentRoom ERROR: {0}", e.Message);
            }        
        }
    
        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void RefreshDeviceConnections()
        {
            Debug.Console(1, this, "RefreshDeviceConnections");
            if (_currentRoom != null)
            {
                var dev = _currentRoom.audio.MasterVolumeControl.CurrentControl;
                if (dev != null) // connect buttons
                {
                    Debug.Console(1, this, "CurrentVolumeControls connect buttons");
                    TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, VolumeUpPress);
                    TriList.SetBoolSigAction(UIBoolJoin.VolumeDownPress, VolumeDownPress);
                    TriList.SetSigFalseAction(UIBoolJoin.Volume1ProgramMutePressAndFB, dev.MuteToggle);
                }
                else
                    Debug.Console(1, this, "CurrentVolumeControls".IsNullString(dev));

                var fbDev = dev as IBasicVolumeWithFeedback;
                if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value].UShortValue = 0;
                else
                {
                    Debug.Console(1, this, "connect feedbacks");
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
            Debug.Console(1, this, "CurrentRoom_CurrentAudioDeviceChange");
            if (args.Type == ChangeType.WillChange)
                ClearDeviceConnections();
            else // did change
                RefreshDeviceConnections();
        }

        /// <summary>
        /// Handler for when a room's volume control list device changes
        /// </summary>
        void CurrentRoom_CurrentAudioDeviceListChanged(object sender, VolumeDeviceChangeEventArgs args)
        {
            Debug.Console(1, this, "CurrentRoom_CurrentAudioDeviceListChanged {0}", args.NewDev.ToString());
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
            Debug.Console(1, this, "VolumeUpPress {0}", state);
            // extend timeouts
            //if (ShowVolumeGauge)
            VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            var dev_ = _currentRoom.audio.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeUp(state);
            else
                Debug.Console(1, this, "CurrentVolumeControls".IsNullString(dev_));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, this, "VolumeDownPress {0}", state);
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            //VolumeButtonsPopupFeedback.BoolValue = state;
            var dev_ = _currentRoom.audio.MasterVolumeControl.CurrentControl;
            if (dev_ != null)
                dev_.VolumeDown(state);
            else
                Debug.Console(1, this, "CurrentVolumeControls".IsNullString(dev_));
        }

        #endregion

    }
}