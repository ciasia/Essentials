using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using CI.Essentials.CouncilChambers;
using CI.Essentials.Power;
using CI.Essentials.PIN;
using CI.Essentials.Utilities;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambersMenuDriver : PanelDriverBase, IKeyed
    {
        string IKeyed.Key { get { return "UILogicDriver"; } }
        //string Key = "UILogicDriver";

        IEssentialsRoom _currentRoom;
        Dictionary<string, ushort> _roomIdx;
        ushort _currentRoomIdx { get; set; }


        BoolFeedback ModePressFeedback;
        BoolFeedback PowerPressFeedback;

        ShutdownFunctionDriver PowerOffDriver;
        PINFunctionDriver PINDriver;

        /// <summary>
        /// 
        /// </summary>
        JoinedSigInterlock PagesInterlock { get; set; }

        public EssentialsCouncilChambersMenuDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Debug.Console(1, this, "Loading");
            _roomIdx = new Dictionary<string, ushort>
            {
                { "room1", 0},
                { "room2", 1},
                { "room3", 2},
            };
            _currentRoomIdx = 0; // todo
            PagesInterlock = new JoinedSigInterlock(parent.TriList);
            PagesInterlock.StatusChanged += new EventHandler<StatusChangedEventArgs>(PagesInterlock_StatusChanged);

            PowerOffDriver = new ShutdownFunctionDriver(this);
            PINDriver = new PINFunctionDriver(this);

            Initialise();
        }

        private void Initialise()
        {
            Debug.Console(1, this, "Initialise");
            // top menu button visibility
            TriList.SetBool(CoP_DigJoins.HOME[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.USER[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.STREAM[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.MODE[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.COMBINE[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.CONFIDENTIAL[CoP_Joins.VIS_IDX], true);

            //top menu actions
            TriList.SetSigFalseAction(CoP_DigJoins.HOME[CoP_Joins.PRESS_IDX], HomePress);
            TriList.SetSigFalseAction(CoP_DigJoins.USER[CoP_Joins.PRESS_IDX], () => { Press("USER"); });
            TriList.SetSigFalseAction(CoP_DigJoins.STREAM[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_STREAMING); });
            TriList.SetSigFalseAction(CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_MODES); });
            TriList.SetSigFalseAction(CoP_DigJoins.COMBINE[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_CONFIRM); });

            // top menu feedback
            ModePressFeedback = new BoolFeedback(() => { return TriList.BooleanInput[CoP_DigJoins.SUB_MODES].BoolValue; });
            ModePressFeedback.LinkInputSig(TriList.BooleanInput[CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX]]);

            //PowerPressFeedback = new BoolFeedback(() => { return TriList.BooleanInput[CoP_DigJoins.SUB_YES_NO].BoolValue && confirmMode == ConfirmMode.Power; });
            //PowerPressFeedback.LinkInputSig(TriList.BooleanInput[CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX]]);

            // bottom menu buttons
            //TriList.SetBool(CoP_DigJoins.POWER[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.HELP[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.LIGHTS[CoP_Joins.VIS_IDX], false);
            TriList.SetBool(CoP_DigJoins.MUSIC[CoP_Joins.VIS_IDX], true);
            TriList.SetBool(CoP_DigJoins.MICS[CoP_Joins.VIS_IDX], true);

            //bottom menu actions
            //TriList.SetSigFalseAction(CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX], PowerPress);
            TriList.SetSigFalseAction(CoP_DigJoins.HELP[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_HELP[0]); });
            TriList.SetSigFalseAction(CoP_DigJoins.LIGHTS[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_LIGHTS); });
            TriList.SetSigFalseAction(CoP_DigJoins.MUSIC[CoP_Joins.PRESS_IDX], () => { Press("MUSIC"); });
            TriList.SetSigFalseAction(CoP_DigJoins.MICS[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_MICS); });

            //TriList.SetSigFalseAction(CoP_DigJoins.CONFIRM_YES, ShutDown);
            //TriList.SetSigFalseAction(CoP_DigJoins.CONFIRM_NO, PagesInterlock.HideAndClear);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, "+++++++++++++++++++++++++++++++++++");
            Debug.Console(1, this, "Show");

            PowerOffDriver.Show();
        }
        
        
        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "DisconnectCurrentRoom");
            _currentRoom = room;
            if(_roomIdx.ContainsKey(room.Key))
            {
                _currentRoomIdx = _roomIdx[room.Key];
                Debug.Console(1, this, "DisconnectCurrentRoom: {0}", _currentRoomIdx);
            }

            if (_currentRoom != null)
            {
                ClearDeviceConnections();
            }
            PowerOffDriver.DisconnectCurrentRoom(room);
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current device
        /// </summary>
        public void ClearDeviceConnections()
        {
            Debug.Console(1, this, "ClearDeviceConnections");
            //TriList.ClearBoolSigAction();
            TriList.SetBool(CoP_DigJoins.SUB_TOP_BAR[_currentRoomIdx], false);
            TriList.SetBool(CoP_DigJoins.SUB_BTM_BAR[_currentRoomIdx], false);
            TriList.SetBool(CoP_DigJoins.SUB_HOME[_currentRoomIdx], false);
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            _currentRoom = room;
            Debug.Console(1, this, "_CurrentRoom".IsNullString(_currentRoom));

            if (_currentRoom != null)
            {
                Debug.Console(1, this, "subscribing to CurrentVolumeDeviceChange");
                RefreshDeviceConnections();
            }
            PowerOffDriver.ConnectCurrentRoom(room);
            PINDriver.ConnectCurrentRoom(room);
        }
    
        /// <summary>
        /// Attaches the buttons and feedback to the room's current device
        /// </summary>
        public void RefreshDeviceConnections()
        {
            Debug.Console(1, "++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.Console(1, this, "RefreshDeviceConnections");
            if (_currentRoom != null)
            {
                TriList.SetBool(CoP_DigJoins.SUB_TOP_BAR[_currentRoomIdx], true);
                TriList.SetBool(CoP_DigJoins.SUB_BTM_BAR[_currentRoomIdx], true);
                //TriList.SetBool(CoP_DigJoins.SUB_HOME[_currentRoomIdx], true);

                //// text
                TriList.SetString(CoP_SerJoins.ROOM_NAME, _currentRoom.Name);
                TriList.SetString(CoP_SerJoins.ROOM_MODE, _currentRoom.IsWarmingUpFeedback.StringValue);
            }
        }

        public void Press(string arg)
        {
            Debug.Console(1, this, "{0} Pressed", arg);
        }

        public void HomePress()
        {
            Debug.Console(1, this, "HomePress");
        }

        public void PowerPress()
        {
            Debug.Console(1, this, "PowerPress");
            //confirmMode = ConfirmMode.Power;
            //PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_YES_NO);
            //TriList.SetString(CoP_SerJoins.CONFIRM_TXT, "Power pressed.\rAre you sure you want to shut down the room?");
            //TriList.SetBool(CoP_DigJoins.COUNTDOWN_VIS, true);
            //TriList.SetUshort(CoP_AnaJoins.COUNTDOWN_BAR, ushort.MaxValue);
        }

        public void ShutDown()
        {
            PagesInterlock.HideAndClear();
            PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_PIN[0]);
        }

        void PagesInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            // need to trigger FireUpdate() to make the buttons actually update feedback state
            ModePressFeedback.FireUpdate();
            PowerPressFeedback.FireUpdate();
        }

    }
}