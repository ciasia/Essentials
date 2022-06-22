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

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambersMenuDriver : PanelDriverBase
    {
        IEssentialsRoom _currentRoom;
        Dictionary<string, ushort> _roomIdx;
        ushort _currentRoomIdx { get; set; }

        string classname = "UILogicDriver";

        /// <summary>
        /// 
        /// </summary>
        JoinedSigInterlock PagesInterlock { get; set; }

        public EssentialsCouncilChambersMenuDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            _roomIdx = new Dictionary<string,ushort>
            {
                { "room1", 0},
                { "room2", 1},
                { "room3", 2},
            };
            _currentRoomIdx = 0; // todo
            PagesInterlock = new JoinedSigInterlock(parent.TriList);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            Debug.Console(1, "{0}, Show", classname);
        }
        
        
        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, "{0}, DisconnectCurrentRoom", classname);
            _currentRoom = room;
            if(_roomIdx.ContainsKey(room.Key))
            {
                _currentRoomIdx = _roomIdx[room.Key];
                Debug.Console(1, "{0}, DisconnectCurrentRoom: {1}", classname, _currentRoomIdx);
            }

            if (_currentRoom != null)
            {
                ClearDeviceConnections();
            }
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current device
        /// </summary>
        public void ClearDeviceConnections()
        {
            Debug.Console(1, "{0}, ClearDeviceConnections", classname);

            //top menu actions
            TriList.ClearBoolSigAction(CoP_DigJoins.HOME[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.USER[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.STREAM[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.COMBINE[CoP_Joins.PRESS_IDX]);

            //bottom menu actions
            TriList.ClearBoolSigAction(CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.HELP[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.LIGHTS[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.MUSIC[CoP_Joins.PRESS_IDX]);
            TriList.ClearBoolSigAction(CoP_DigJoins.MICS[CoP_Joins.PRESS_IDX]);
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, "{0}, ConnectCurrentRoom", classname);
            _currentRoom = room;
            Debug.Console(1, "{0}, _CurrentRoom {1}= null", classname, _currentRoom == null ? "=" : "!");

            if (_currentRoom != null)
            {
                Debug.Console(1, "{0}, subscribing to CurrentVolumeDeviceChange", classname);
                RefreshDeviceConnections();
            } 
        }
    
        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void RefreshDeviceConnections()
        {
            Debug.Console(1, "++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.Console(1, "{0}, RefreshDeviceConnections", classname);
            if (_currentRoom != null)
            {
                TriList.SetBool(CoP_DigJoins.SUB_TOP_BAR[_currentRoomIdx], true);
                TriList.SetBool(CoP_DigJoins.SUB_BTM_BAR[_currentRoomIdx], true);
                TriList.SetBool(CoP_DigJoins.SUB_HOME[_currentRoomIdx], true);

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
                TriList.SetSigFalseAction(CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX], ModePress);
                TriList.SetSigFalseAction(CoP_DigJoins.COMBINE[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_CONFIRM); });

                // bottom menu buttons
                TriList.SetBool(CoP_DigJoins.POWER[CoP_Joins.VIS_IDX], true);
                TriList.SetBool(CoP_DigJoins.HELP[CoP_Joins.VIS_IDX], true);
                TriList.SetBool(CoP_DigJoins.LIGHTS[CoP_Joins.VIS_IDX], false);
                TriList.SetBool(CoP_DigJoins.MUSIC[CoP_Joins.VIS_IDX], true);
                TriList.SetBool(CoP_DigJoins.MICS[CoP_Joins.VIS_IDX], true);

                //bottom menu actions
                TriList.SetSigFalseAction(CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX], () => { Press("POWER"); });
                TriList.SetSigFalseAction(CoP_DigJoins.HELP[CoP_Joins.PRESS_IDX], () => { Press("HELP"); });
                TriList.SetSigFalseAction(CoP_DigJoins.LIGHTS[CoP_Joins.PRESS_IDX], () => { Press("LIGHTS"); });
                TriList.SetSigFalseAction(CoP_DigJoins.MUSIC[CoP_Joins.PRESS_IDX], () => { Press("MUSIC"); });
                TriList.SetSigFalseAction(CoP_DigJoins.MICS[CoP_Joins.PRESS_IDX], () => { Press("MICS"); });

                // text
                TriList.SetString(CoP_SerJoins.ROOM_NAME, _currentRoom.Name);
                TriList.SetString(CoP_SerJoins.ROOM_MODE, "System is off");
            }
        }

        public void Press(string arg)
        {
            Debug.Console(1, "{0}, {1} Pressed", classname, arg);
        }

        public void HomePress()
        {
            Debug.Console(1, "{0}, HomePress", classname);
        }

        public void ModePress()
        {
            var b = TriList.GetBool(CoP_DigJoins.SUB_MODES);
            TriList.SetBool(CoP_DigJoins.SUB_MODES, !b);
            PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_MODES);
        }

    }
}