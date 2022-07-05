using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using PepperDash.Essentials.Core.SmartObjects;
using CI.Essentials.Utilities;

namespace CI.Essentials.PIN
{
    public enum eUserLevel
    {
        None,
        Unauthorised,
        User,
        Operator,
        Admin
    };

    public class UserEventArgs
    {
        public UserEventArgs(eUserLevel level) 
        { 
            Level = level; 
        }
        public eUserLevel Level { get; private set; }
    }

    public class PINFunctionDriver : PanelDriverBase, IEssentialsConnectableRoomDriver, IKeyed
    {
        string IKeyed.Key { get { return "MainUIDriver"; } }
        //string Key = "MainUIDriver";

        public delegate void UserEventHandler(object sender, UserEventArgs e);
        public event UserEventHandler UserEvent;

        /// <summary>
        /// 
        /// </summary>
        IEssentialsRoom _CurrentRoom;
        PanelDriverBase Parent;
        
        CTimer PinAuthorizedTimer;
        StringBuilder PinEntryBuilder = new StringBuilder(4);
        //bool IsAuthorized;
        public eUserLevel AuthorizationLevel { get; private set; }
        SmartObjectNumeric PinKeypad;
        Dictionary<eUserLevel, string> Passwords;

        public PINFunctionDriver(PanelDriverBase parent) 
            : base(parent.TriList)
        {
            Debug.Console(1, this, "=====================================");
            Debug.Console(1, this, "Loading");
            //Config = config;
            Parent = parent;

            AuthorizationLevel = eUserLevel.None;
            Passwords = new Dictionary<eUserLevel, string>();
            Passwords.Add(eUserLevel.Admin, "1988");
            Passwords.Add(eUserLevel.User, "1234");
            Passwords.Add(eUserLevel.Operator, "5678");

            Initialise();
            Debug.Console(1, this, "=====================================");
        }

        private void Initialise()
        {
            Debug.Console(1, this, "Initialise");
            //TriList.SetSigFalseAction(UIBoolJoin.ShowPowerOffPress, PowerButtonPressed);
            SetupPinModal();
        }

        /// <summary>
        /// Wire up the keypad and buttons
        /// </summary>
        void SetupPinModal()
        {
            TriList.SetSigFalseAction(UIBoolJoin.PinDialogCancelPress, CancelPinDialog);
            PinKeypad = new SmartObjectNumeric(TriList.SmartObjects[UISmartObjectJoin.TechPinDialogKeypad], true);
            PinKeypad.Digit0.UserObject = new Action<bool>(b => { if (b)DialPinDigit('0'); });
            PinKeypad.Digit1.UserObject = new Action<bool>(b => { if (b)DialPinDigit('1'); });
            PinKeypad.Digit2.UserObject = new Action<bool>(b => { if (b)DialPinDigit('2'); });
            PinKeypad.Digit3.UserObject = new Action<bool>(b => { if (b)DialPinDigit('3'); });
            PinKeypad.Digit4.UserObject = new Action<bool>(b => { if (b)DialPinDigit('4'); });
            PinKeypad.Digit5.UserObject = new Action<bool>(b => { if (b)DialPinDigit('5'); });
            PinKeypad.Digit6.UserObject = new Action<bool>(b => { if (b)DialPinDigit('6'); });
            PinKeypad.Digit7.UserObject = new Action<bool>(b => { if (b)DialPinDigit('7'); });
            PinKeypad.Digit8.UserObject = new Action<bool>(b => { if (b)DialPinDigit('8'); });
            PinKeypad.Digit9.UserObject = new Action<bool>(b => { if (b)DialPinDigit('9'); });
            PinKeypad.Misc1.UserObject  = new Action<bool>(b => { if (b)DialPinClear(); });
            PinKeypad.Misc2.UserObject  = new Action<bool>(b => { if (b)DialPinBackspace(); });
        }

        void DialPinBackspace()
        {
            Debug.Console(1, this, "DialPinBackspace");
            PinEntryBuilder.Remove(PinEntryBuilder.Length-1, 1);
            var len = PinEntryBuilder.Length;
            SetPinDotsFeedback(len);
            Debug.Console(1, this, "DialPinBackspace {0}", PinEntryBuilder.ToString());
        }

        void DialPinClear()
        {
            Debug.Console(1, this, "DialPinClear");
            var len = PinEntryBuilder.Length;
            PinEntryBuilder.Remove(0, len); // clear it either way
            SetPinDotsFeedback(0);
            Debug.Console(1, this, "DialPinClear {0}", PinEntryBuilder.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        void DialPinDigit(char d)
        {
            Debug.Console(1, this, "DialPinDigit {0}", d);
            PinEntryBuilder.Append(d);
            var len = PinEntryBuilder.Length;
            SetPinDotsFeedback(len);
            Debug.Console(1, this, "PinEntryBuilder: {0}", PinEntryBuilder.ToString());

            // check it!
            if (len == 4)
            {
                Debug.Console(1, this, "DialPinDigit user: {0}", eUserLevel.User.ToString());

                var auth = Passwords.First(x => x.Value == PinEntryBuilder.ToString());
                if (auth.Value == PinEntryBuilder.ToString())
                {
                    AuthorizationLevel = auth.Key;
                    TriList.SetBool(UIBoolJoin.PinDialog4DigitVisible, false);
                    //Show();
                }
                else
                {
                    AuthorizationLevel = eUserLevel.None;
                    TriList.SetBool(UIBoolJoin.PinDialogErrorVisible, true);
                    new CTimer(o =>
                    {
                        TriList.SetBool(UIBoolJoin.PinDialogErrorVisible, false);
                    }, 1500);
                }
                DialPinClear();
                if (UserEvent != null)
                    UserEvent(this, new UserEventArgs(AuthorizationLevel));
            }
        }

        /// <summary>
        /// Draws the dots as pin is entered
        /// </summary>
        /// <param name="len"></param>
        void SetPinDotsFeedback(int len)
        {
            TriList.SetBool(UIBoolJoin.PinDialogDot1, len >= 1);
            TriList.SetBool(UIBoolJoin.PinDialogDot2, len >= 2);
            TriList.SetBool(UIBoolJoin.PinDialogDot3, len >= 3);
            TriList.SetBool(UIBoolJoin.PinDialogDot4, len == 4);

        }

        /// <summary>
        /// Does what it says
        /// </summary>
        void CancelPinDialog()
        {
            PinEntryBuilder.Remove(0, PinEntryBuilder.Length);
            TriList.SetBool(UIBoolJoin.PinDialog4DigitVisible, false);
            //IsAuthorized = eUserLevel.None;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()//JoinedSigInterlock pages_interlock)//, uint join)
        {
            Debug.Console(1, this, "Show");
            // divert to PIN if we need auth
            if (AuthorizationLevel != eUserLevel.None)
            {
                // Cancel the auth timer so we don't deauth after coming back in
                if (PinAuthorizedTimer != null)
                    PinAuthorizedTimer.Stop();

                //TriList.SetBool(UIBoolJoin.TechCommonItemsVisbible, true);
                //Parent.PagesInterlock.Show();
                TriList.SetBool(UIBoolJoin.PinDialog4DigitVisible, false);
                base.Show();
            }
            else
            {
                //pages_interlock.ShowInterlocked(UIBoolJoin.PinDialog4DigitVisible);
                TriList.SetBool(UIBoolJoin.PinDialog4DigitVisible, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Hide()//JoinedSigInterlock pages_interlock)
        {
            // Leave it authorized for 60 seconds.
            if (AuthorizationLevel != eUserLevel.None)
                PinAuthorizedTimer = new CTimer(o =>
                {
                    PinAuthorizedTimer = null;
                    AuthorizationLevel = eUserLevel.None;
                    if (UserEvent != null)
                        UserEvent(this, new UserEventArgs(AuthorizationLevel));
                }, 60000);
            //TriList.SetBool(UIBoolJoin.TechCommonItemsVisbible, false);
            //pages_interlock.HideAndClear();
            base.Hide();
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "DisconnectCurrentRoom");
            _CurrentRoom = room;
        }


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            _CurrentRoom = room;
            Debug.Console(1, this, "_CurrentRoom".IsNullString(_CurrentRoom));

            if (_CurrentRoom != null)
            {
                var room_config = room.Config;
                Debug.Console(1, this, "room_config".IsNullString(room_config));
                var PIN_config = room_config as IPINPropertiesConfig;
                Debug.Console(1, this, "PIN_config".IsNullString(PIN_config ));
                if (PIN_config != null)
                    Passwords[eUserLevel.User] = PIN_config.Password;
            }
        }
    }
}