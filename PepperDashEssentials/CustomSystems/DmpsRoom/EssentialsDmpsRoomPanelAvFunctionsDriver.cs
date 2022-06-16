using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DmpsRoom
{
    public class EssentialsDmpsRoomPanelAvFunctionsDriver : PanelDriverBase
    {
        /// <summary>
        /// The parent driver for this
        /// </summary>
        public PanelDriverBase Parent { get; private set; }

        CrestronTouchpanelPropertiesConfig Config;

        private SmartObject SwitcherInputsSO;
        private SmartObject SwitcherOutputsSO;

        /// <summary>
        /// 
        /// </summary>
        public string DefaultRoomKey { get; set; }

        BoolInputSig ToggleButtonSig;
        BoolInputSig OnlineButtonSig;
        BoolInputSig PowerButtonSig;

        BoolFeedback ToggleIsOnFeedback;

        public EssentialsDmpsRoomPanelAvFunctionsDriver(PanelDriverBase parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Config = config;
            Parent = parent;

            // set button high all the time
            OnlineButtonSig = TriList.BooleanInput[EssentialsDmpsRoomJoins.OnlineButtonVisible];
            //OnlineButtonSig.BoolValue = true;
            TriList.SetBool(EssentialsDmpsRoomJoins.OnlineButtonVisible, true);

            // toggle a button (inc feedback)
            ToggleButtonSig = TriList.BooleanInput[EssentialsDmpsRoomJoins.ToggleButtonPress];
            TriList.SetSigFalseAction(EssentialsDmpsRoomJoins.ToggleButtonPress, ToggleButtonPress);
            
            // create a feedback for something else to link to
            ToggleIsOnFeedback = new BoolFeedback(() => ToggleButtonSig.BoolValue);
            //ToggleIsOnFeedback.OutputChange += (o, a) =>
            //{
            //    Debug.Console(0, "0: ToggleIsOnFeedback.OutputChange: {0}", a.BoolValue);
            //};

            // set the power button feedback to follow the state of the toggle button
            PowerButtonSig = TriList.BooleanInput[EssentialsDmpsRoomJoins.PowerButtonPress];
            //TriList.SetBoolSigAction(EssentialsMinimalRoomJoins.PowerButtonPress, PowerButtonPress);
            ToggleIsOnFeedback.LinkInputSig(PowerButtonSig);


            
        }

        public void ToggleButtonPress()
        {
            Debug.Console(0, "0: ToggleButtonPress, ToggleIsOnFeedback.BoolValue: {0}", ToggleIsOnFeedback.BoolValue);
            //Debug.Console(0, "0: ToggleButtonPress, ToggleButtonSig.BoolValue: {0}", ToggleButtonSig.BoolValue);
            TriList.SetBool(EssentialsDmpsRoomJoins.ToggleButtonPress, !ToggleButtonSig.BoolValue);
            ToggleIsOnFeedback.FireUpdate();
        }

        public void PowerButtonPress()
        {
            Debug.Console(0, "0: PowerButtonPress");
            TriList.SetBool(EssentialsDmpsRoomJoins.PowerButtonPress, !PowerButtonSig.BoolValue);
        }
    }
}