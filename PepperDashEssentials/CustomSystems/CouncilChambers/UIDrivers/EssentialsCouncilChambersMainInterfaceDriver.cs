using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using CI.Essentials.Audio;
using CI.Essentials.Power;
using CI.Essentials.PIN;
using CI.Essentials.Utilities;
using CI.Essentials.Modes;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambersMainInterfaceDriver : PanelDriverBase, IAVDriver, IKeyed
    {
        string IKeyed.Key { get { return "MainUIDriver"; } }
        //string Key = "MainUIDriver";
        
        CrestronTouchpanelPropertiesConfig Config;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        public PanelDriverBase Parent { get; private set; }

        public string DefaultRoomKey { get; set; }

        public IEssentialsCouncilChambers CurrentRoom
        {
            get { return _CurrentRoom; }
            set
            {
                Debug.Console(1, "CurrentRoom setter. value {0}", value.ToString().IsNullString(value));
                SetCurrentRoom(value);
            }
        }
        IEssentialsCouncilChambers _CurrentRoom;

        Dictionary<string, ushort> _roomIdx;
        ushort _currentRoomIdx { get; set; }

        BoolFeedback ModePressFeedback;
        //BoolFeedback PowerPressFeedback;

        /// <summary>
        /// All children attached to this driver.  For hiding and showing as a group.
        /// </summary>
        List<PanelDriverBase> ChildDrivers = new List<PanelDriverBase>();

        AudioPanelFunctionsDriver audio_driver;
        ShutdownFunctionDriver power_off_driver;
        PINFunctionDriver pin_driver;
        SystemModeFunctionDriver mode_driver;

        /// <summary>
        /// 
        /// </summary>
        JoinedSigInterlock PagesInterlock { get; set; }

        public EssentialsCouncilChambersMainInterfaceDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Config = config;
            Parent = parent;

            Debug.Console(1, "Loading");

            audio_driver = new AudioPanelFunctionsDriver(this, config);
            ChildDrivers.Add(audio_driver);

            power_off_driver = new ShutdownFunctionDriver(this);
            ChildDrivers.Add(power_off_driver);

            pin_driver = new PINFunctionDriver(this);
            ChildDrivers.Add(pin_driver);
            pin_driver.UserEvent += new PINFunctionDriver.UserEventHandler(pin_driver_UserEvent);

            mode_driver = new SystemModeFunctionDriver(this);
            ChildDrivers.Add(mode_driver);

            _roomIdx = new Dictionary<string, ushort> // todo, get the base config and extract room names
            {
                { "room1", 0},
                { "room2", 1},
                { "room3", 2},
            };
            _currentRoomIdx = 0;

            PagesInterlock = new JoinedSigInterlock(parent.TriList);
            PagesInterlock.StatusChanged += new EventHandler<StatusChangedEventArgs>(PagesInterlock_StatusChanged);
            Debug.Console(1, this, "Loaded");

            Initialise_MenuButtons();
        }

        void pin_driver_UserEvent(object sender, UserEventArgs e)
        {
            Debug.Console(1, this, "pin_driver_UserEvent {0}", e.Level);
            if (e.Level == eUserLevel.None)
            {
                PagesInterlock.ShowInterlocked(UIBoolJoin.PinDialog4DigitVisible);
            }
            else
            {
                Debug.Console(1, this, "pin_driver_UserEvent, show home index {0}, button {1}", _currentRoomIdx, CoP_DigJoins.SUB_HOME[_currentRoomIdx]);
                PagesInterlock.ShowInterlocked(CoP_DigJoins.SUB_HOME[_currentRoomIdx]);
                Debug.Console(1, this, "##################################");
                Debug.Console(1, this, "pin_driver_UserEvent, showing home");
            }
            Debug.Console(1, this, "pin_driver_UserEvent, Show_MenuButtons");
            Show_MenuButtons();
        }

        private void Show_MenuButtons()
        {
            Debug.Console(1, this, "Show_MenuButtons");
            Debug.Console(1, this, "pin_driver.AuthorizationLevel: {0}", pin_driver.AuthorizationLevel);
            var visible = pin_driver.AuthorizationLevel != eUserLevel.None;
            // top menu button visibility
            TriList.SetBool(CoP_DigJoins.HOME[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.USER[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.STREAM[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.MODE[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.COMBINE[CoP_Joins.VIS_IDX], pin_driver.AuthorizationLevel == eUserLevel.Operator);
            TriList.SetBool(CoP_DigJoins.CONFIDENTIAL[CoP_Joins.VIS_IDX], visible);

            // bottom menu button visibility
            TriList.SetBool(CoP_DigJoins.POWER[CoP_Joins.VIS_IDX], visible && _CurrentRoom.OnFeedback.BoolValue); // this may change with system power
            TriList.SetBool(CoP_DigJoins.HELP[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.MUSIC[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.MICS[CoP_Joins.VIS_IDX], visible);
            TriList.SetBool(CoP_DigJoins.LIGHTS[CoP_Joins.VIS_IDX], pin_driver.AuthorizationLevel == eUserLevel.Operator);
            TriList.SetBool(CoP_DigJoins.SUB_OPERATOR, pin_driver.AuthorizationLevel == eUserLevel.Operator);

            TriList.SetBool(CoP_DigJoins.LOGO[CoP_Joins.VIS_IDX], true);
        }

        private void Initialise_MenuButtons()
        {
            Debug.Console(1, this, "Initialise_MenuButtons");
            Show_MenuButtons();

            //top menu actions
            TriList.SetSigFalseAction(CoP_DigJoins.HOME[CoP_Joins.PRESS_IDX], HomePress);
            TriList.SetSigFalseAction(CoP_DigJoins.USER[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(UIBoolJoin.PinDialog4DigitVisible); });
            TriList.SetSigFalseAction(CoP_DigJoins.STREAM[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_STREAMING); });
            TriList.SetSigFalseAction(CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_MODES); });
            TriList.SetSigFalseAction(CoP_DigJoins.COMBINE[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_CONFIRM); });

            // top menu feedback
            ModePressFeedback = new BoolFeedback(() => { return TriList.BooleanInput[CoP_DigJoins.SUB_MODES].BoolValue; });
            ModePressFeedback.LinkInputSig(TriList.BooleanInput[CoP_DigJoins.MODE[CoP_Joins.PRESS_IDX]]);

            //PowerPressFeedback = new BoolFeedback(() => { return TriList.BooleanInput[CoP_DigJoins.SUB_YES_NO].BoolValue && confirmMode == ConfirmMode.Power; });
            //PowerPressFeedback.LinkInputSig(TriList.BooleanInput[CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX]]);

            //bottom menu actions
            //TriList.SetSigFalseAction(CoP_DigJoins.POWER[CoP_Joins.PRESS_IDX], PowerPress);
            TriList.SetSigFalseAction(CoP_DigJoins.HELP[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_HELP[0]); });
            TriList.SetSigFalseAction(CoP_DigJoins.LIGHTS[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_LIGHTS); });
            TriList.SetSigFalseAction(CoP_DigJoins.MUSIC[CoP_Joins.PRESS_IDX], () => { Press("MUSIC"); });
            TriList.SetSigFalseAction(CoP_DigJoins.MICS[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_MICS); });

            TriList.SetSigFalseAction(CoP_DigJoins.LOGO[CoP_Joins.PRESS_IDX], () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_PIN[0]); });
            //TriList.SetSigFalseAction(CoP_DigJoins.CONFIRM_YES, ShutDown);
            Debug.Console(1, this, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");

            //operator menu actions
            TriList.SetSigFalseAction(CoP_DigJoins.VIDEO_MATRIX, () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_VIDEO_MATRIX); });
            TriList.SetSigFalseAction(CoP_DigJoins.SCHEDULE, () => { PagesInterlock.ShowInterlockedWithToggle(CoP_DigJoins.SUB_SCHEDULE); });
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, this, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            Debug.Console(1, this, "Show");
            if (CurrentRoom == null)
            {
                Debug.Console(1, this, "ERROR: Cannot show. No room assigned");
                return;
            }

            var roomConf = CurrentRoom.PropertiesConfig;

            //foreach (var d in ChildDrivers)
            //    d.Show();
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
            Debug.Console(1, this, "room_ConfigChanged");
            RefreshCurrentRoom(_CurrentRoom);
        }

        /// <summary>
        /// Helper for property setter. Sets the panel to the given room, latching up all functionality
        /// </summary>
        public void RefreshCurrentRoom(IEssentialsCouncilChambers room)
        {
            try
            {
                Debug.Console(1, this, "RefreshCurrentRoom");
                Debug.Console(1, this, "room".IsNullString(room));
                if (_CurrentRoom != null)
                {
                    Debug.Console(1, this, "DisconnectCurrentRoom: {0}", _currentRoomIdx);
                    foreach (var c in ChildDrivers)
                    {
                        var d = c as IEssentialsConnectableRoomDriver;
                        if(d != null)
                            d.DisconnectCurrentRoom(_CurrentRoom);
                    }
                    ClearDeviceConnections();
                }
                //else
                //    Debug.Console(1, "_CurrentRoom == null");
                _CurrentRoom = room;
                if (_CurrentRoom != null)
                    ConnectCurrentRoom();
                else
                {
                    Debug.Console(1, this, "no room selected");
                    // Clear sigs that need to be
                    TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = "Select a room";
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "RefreshCurrentRoom ERROR: {0}", e.Message);
            }

        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom()
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            Debug.Console(1, this, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            Debug.Console(1, this, "_CurrentRoom".IsNullString(_CurrentRoom));

            if (_CurrentRoom != null)
            {
                if (_roomIdx != null && _roomIdx.ContainsKey(_CurrentRoom.Key))
                    _currentRoomIdx = _roomIdx[_CurrentRoom.Key];

                // Name and logo
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = _CurrentRoom.Name;

                foreach (var c in ChildDrivers)
                {
                    var d = c as IEssentialsConnectableRoomDriver;
                    if (d != null)
                    {
                        Debug.Console(1, this, "ConnectCurrentRoom is an IEssentialsConnectableRoom");
                        d.ConnectCurrentRoom(_CurrentRoom);
                    }
                    else
                    {
                        Debug.Console(1, this, "ConnectCurrentRoom is NOT an IEssentialsConnectableRoom");
                        var e = c as IEssentialsRoom;
                        Debug.Console(1, this, "ConnectCurrentRoom: {0}", e.Key);
                    }

                }
                RefreshDeviceConnections();
            }
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
        /// Attaches the buttons and feedback to the room's current device
        /// </summary>
        public void RefreshDeviceConnections()
        {
            Debug.Console(1, this, "++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.Console(1, this, "RefreshDeviceConnections");
            if (_CurrentRoom != null)
            {
                TriList.SetBool(CoP_DigJoins.SUB_TOP_BAR[_currentRoomIdx], true);
                TriList.SetBool(CoP_DigJoins.SUB_BTM_BAR[_currentRoomIdx], true);
                //TriList.SetBool(CoP_DigJoins.SUB_HOME[_currentRoomIdx], true);

                //// text
                TriList.SetString(CoP_SerJoins.ROOM_NAME, _CurrentRoom.Name);
                //TriList.SetString(CoP_SerJoins.ROOM_MODE, _CurrentRoom.IsWarmingUpFeedback.StringValue);

                pin_driver.Show();//PagesInterlock);//, CoP_DigJoins.SUB_PIN[0]);
            }
        }

        void SetCurrentRoom(IEssentialsCouncilChambers room)
        {
            Debug.Console(1, this, "SetCurrentRoom");
            //Debug.Console(1, "SetCurrentRoom, _CurrentRoom {1}= null", _CurrentRoom == null ? "=" : "!");
            Debug.Console(1, this, "SetCurrentRoom, {0}", "room".IsNullString(room));
            if (_CurrentRoom == room) return;
            // Disconnect current (probably never called)
            if (_CurrentRoom != null)
                _CurrentRoom.ConfigChanged -= room_ConfigChanged;
            RefreshCurrentRoom(room); // sets _CurrentRoom to room
            if (_CurrentRoom != null)
                _CurrentRoom.ConfigChanged += room_ConfigChanged;
        }

        #endregion


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

        void PagesInterlock_StatusChanged(object sender, StatusChangedEventArgs args)
        {
            Debug.Console(1, this, "PagesInterlock_StatusChanged {0}", args.NewJoin);
            try
            {
                // need to trigger FireUpdate() to make the buttons actually update feedback state
                if (ModePressFeedback != null)
                    ModePressFeedback.FireUpdate();
                //if (PowerPressFeedback != null)
                //    PowerPressFeedback.FireUpdate();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "PagesInterlock_StatusChanged ERROR: {0}", e.Message);
            }
        }

        #region IAVDriver Members
        // need IAVDriver so class can be loaded in EssentialsTouchPanelController

        public void HideNotificationRibbon()
        {
            Debug.Console(2, this, "HideNotificationRibbon");
        }

        public JoinedSigInterlock PopupInterlock { get; private set; }

        public void ShowNotificationRibbon(string message, int timeout)
        {
            Debug.Console(2, this, "ShowNotificationRibbon");
        }

        public void ShowTech()
        {
            Debug.Console(2, this, "ShowTech");
        }

        public uint StartPageVisibleJoin { get; private set; }

        #endregion
    }
}