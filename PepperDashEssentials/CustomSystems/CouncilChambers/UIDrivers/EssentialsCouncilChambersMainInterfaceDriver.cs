using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using CI.Essentials.Audio;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambersMainInterfaceDriver : PanelDriverBase, IAVDriver
    {
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
                SetCurrentRoom(value);
            }
        }
        IEssentialsCouncilChambers _CurrentRoom;

        AudioPanelFunctionsDriver audio_driver;
        EssentialsCouncilChambersMenuDriver menu_driver;
        string classname = "MainUIDriver";

        public EssentialsCouncilChambersMainInterfaceDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Config = config;
            Parent = parent;

            Debug.Console(1, "{0}, Loading", classname);

            audio_driver = new AudioPanelFunctionsDriver(this, config);
            menu_driver = new EssentialsCouncilChambersMenuDriver(this, config);
            Debug.Console(1, "{0}, Loaded", classname);
            Debug.Console(1, "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            Debug.Console(1, "{0}, Show", classname);
            if (CurrentRoom == null)
            {
                Debug.Console(1, "{0}, ERROR: Cannot show. No room assigned", classname);
                return;
            }

            var roomConf = CurrentRoom.PropertiesConfig;

            audio_driver.Show();
            menu_driver.Show();

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
            Debug.Console(1, "{0}, room_ConfigChanged", classname);
            RefreshCurrentRoom(_CurrentRoom);
        }

        /// <summary>
        /// Helper for property setter. Sets the panel to the given room, latching up all functionality
        /// </summary>
        public void RefreshCurrentRoom(IEssentialsCouncilChambers room)
        {
            Debug.Console(1, "{0}, RefreshCurrentRoom", classname);
            if (_CurrentRoom != null)
            {
                audio_driver.DisconnectCurrentRoom(_CurrentRoom as IEssentialsAudioRoom);
                menu_driver.DisconnectCurrentRoom(_CurrentRoom as IEssentialsRoom);
            }
            //else
            //    Debug.Console(1, "{0}, _CurrentRoom == null", classname);

            _CurrentRoom = room;

            if (_CurrentRoom != null)
            {
                // Name and logo
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = _CurrentRoom.Name;
                //Debug.Console(1, "{0}, _CurrentRoom {1}= null", classname, _CurrentRoom == null ? "=" : "!");
                var room_ = _CurrentRoom as IEssentialsAudioRoom;
                //Debug.Console(1, "{0}, room_ as IEssentialsAudioRoom {1}= null", classname, room_ == null ? "=" : "!");
                audio_driver.ConnectCurrentRoom(_CurrentRoom as IEssentialsAudioRoom);
                menu_driver.ConnectCurrentRoom(_CurrentRoom as IEssentialsRoom);
            }
            else
            {
                Debug.Console(1, "{0}, subscribing to no room selected", classname);

                // Clear sigs that need to be
                TriList.StringInput[UIStringJoin.CurrentRoomName].StringValue = "Select a room";
            }
        }

        void SetCurrentRoom(IEssentialsCouncilChambers room)
        {
            Debug.Console(1, "{0}, SetCurrentRoom", classname);
            //Debug.Console(1, "_CurrentRoom {0}= null", _CurrentRoom == null ? "=" : "!");
            Debug.Console(1, "{0}, new room {1}= null", classname, room == null ? "=" : "!");
            if (_CurrentRoom == room) return;
            // Disconnect current (probably never called)

            if (_CurrentRoom != null)
                _CurrentRoom.ConfigChanged -= room_ConfigChanged;

            room.ConfigChanged -= room_ConfigChanged;
            room.ConfigChanged += room_ConfigChanged;

            RefreshCurrentRoom(room);
        }

        #endregion


        #region IAVDriver Members
        // need IAVDriver so class can be loaded in EssentialsTouchPanelController

        public void HideNotificationRibbon()
        {
            Debug.Console(2, "{0}, HideNotificationRibbon", classname);
        }

        public JoinedSigInterlock PopupInterlock { get; private set; }

        public void ShowNotificationRibbon(string message, int timeout)
        {
            Debug.Console(2, "{0}, ShowNotificationRibbon", classname);
        }

        public void ShowTech()
        {
            Debug.Console(2, "{0}, ShowTech", classname);
        }

        public uint StartPageVisibleJoin { get; private set; }

        #endregion
    }
}