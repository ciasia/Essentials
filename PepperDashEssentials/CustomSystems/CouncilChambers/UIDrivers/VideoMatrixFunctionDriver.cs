using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Crestron.SimplSharpPro;
using PepperDash.Essentials;
using PepperDash.Core;
using CI.Essentials.Utilities;
using CI.Essentials.UI;
using CI.Essentials.CouncilChambers;

namespace CI.Essentials.Video
{
    public class VideoMatrixFunctionDriver : PanelDriverBase, IEssentialsConnectableRoomDriver, IKeyed
    {
        string IKeyed.Key { get { return "VideoUIDriver"; } }
        //string Key = "VideoUIDriver";

        private IEssentialsRoom _currentRoom;
        private SmartObject InputsSO;
        private SmartObject OutputsSO;

        public SubpageReferenceList InputsSrl { get; set; }
        public SubpageReferenceList OutputsSrl { get; set; }

        private uint _inputListCount;
        private uint _outputListCount;

        BoolInputSig AudioFlagSig;
        BoolFeedback AudioFlagFeedback;

        BoolInputSig VideoFlagSig;
        BoolFeedback VideoFlagFeedback;

        BoolInputSig EnterSig;
        BoolInputSig CancelSig;

        public VideoMatrixFunctionDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Debug.Console(1, this, "Loading");
            InputsSO = TriList.SmartObjects[SmartJoins.vidMatrixInputsSrl];
            Debug.Console(2, this, "InputsSO".IsNullString(InputsSO));
            InputsSrl = new SubpageReferenceList(TriList, SmartJoins.vidMatrixInputsSrl, 2, 1, 1);
            Debug.Console(2, this, "InputsSrl".IsNullString(InputsSrl));

            AudioFlagSig = TriList.BooleanInput[CoP_DigJoins.MATRIX_AUDIO];
            AudioFlagFeedback = SetupTogglingSig(AudioFlagSig);
            //var fb = new BoolFeedback(() => sig.BoolValue);
            //AudioFlagFeedback = fb;
            //TriList.SetSigFalseAction(sig.Number, () => ToggleSig(sig.Number, sig, fb));
            //SetupTogglingSig(CoP_DigJoins.MATRIX_AUDIO, AudioFlagSig, AudioFlagFeedback);

            VideoFlagSig = TriList.BooleanInput[CoP_DigJoins.MATRIX_VIDEO];
            VideoFlagFeedback = SetupTogglingSig(VideoFlagSig);

            EnterSig = TriList.BooleanInput[CoP_DigJoins.MATRIX_ENTER];
            TriList.SetSigFalseAction(EnterSig.Number, EnterPress);

            CancelSig = TriList.BooleanInput[CoP_DigJoins.MATRIX_CANCEL];
            TriList.SetSigFalseAction(CancelSig.Number, CancelPress);
        }

        BoolFeedback SetupTogglingSig(BoolInputSig sig)
        {
            var fb = new BoolFeedback(() => sig.BoolValue);
            TriList.SetSigFalseAction(sig.Number, () => ToggleSig(sig.Number, sig, fb));
            return fb;
        }

        //void SetupTogglingSig(uint join, BoolInputSig press_sig, BoolFeedback feedback_sig)
        //{
        //    press_sig = TriList.BooleanInput[join];
        //    feedback_sig = new BoolFeedback(() => press_sig.BoolValue);
        //    TriList.SetSigFalseAction(join, () => ToggleSig(join, press_sig, feedback_sig));
        //}

        void ToggleSig(uint join, BoolInputSig sig, BoolFeedback fb)
        {
            TriList.SetBool(join, !sig.BoolValue);
            fb.FireUpdate();
        }

        void EnterPress()
        {
            Debug.Console(2, this, "EnterPress");
        }

        void CancelPress()
        {
            Debug.Console(2, this, "CancelPress");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, this, "Show");
            // Attach actions
        }


        /// <summary>
        /// SRL - connect the joins to funcs
        /// </summary>
        void RefreshInputList()
        {
            try
            {
                Debug.Console(1, this, "RefreshInputList");
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "RefreshInputList ERROR: {0}", e.Message);
            }
        }

        /// <summary>
        /// SRL - connect the joins to funcs
        /// </summary>
        void RefreshOutputList()
        {
            try
            {
                Debug.Console(1, this, "RefreshOutputList");
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "RefreshOutputList ERROR: {0}", e.Message);
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
                _currentRoom = (IEssentialsRoom)room;
                //_currentRoom = room as IEssentialsAudioRoom;

                if (_currentRoom != null)
                {
                    // Disconnect current room
                    //_currentRoom.audio.MasterVolumeDeviceChange -= this.CurrentRoom_CurrentMasterAudioDeviceChange;
                    ClearDeviceConnections();
                }
                else
                    Debug.Console(1, this, "CurrentRoom".IsNullString(_currentRoom));
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "DisconnectCurrentRoom ERROR: {0}", e.Message);
            }

        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current device
        /// </summary>
        public void ClearDeviceConnections()
        {
            Debug.Console(1, this, "ClearDeviceConnections");

            //TriList.ClearBoolSigAction(UIBoolJoin.VolumeUpPress);
            //InputsSrl.Clear();
            //OutputsSrl.Clear();
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            try
            {
                _currentRoom = (IEssentialsRoom)room;
                //Debug.Console(1, this, "_CurrentRoom {1}= null", classname, _currentRoom == null ? "=" : "!");

                if (_currentRoom != null)
                {
                    Debug.Console(1, this, "subscribing to events");
                    //_currentRoom.audio.MasterVolumeDeviceChange += CurrentRoom_CurrentMasterAudioDeviceChange;
                    //_currentRoom.audio.VolumeDeviceListChange += CurrentRoom_CurrentAudioDeviceListChanged;
                    RefreshDeviceConnections();
                }
                else
                    Debug.Console(1, this, "CurrentRoom".IsNullString(_currentRoom));
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
                RefreshInputList();
                RefreshOutputList();
            }
        }
    }
}