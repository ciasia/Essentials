using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// used for AudioPanelFunctionsDriver to pass the audio properties to a room
    /// </summary>
    public interface IEssentialsModalRoom : IEssentialsRoom
    {
        ModesController modes { get; }
        //ModeListItem CurrentModeInfo { get; set; }
        //event ModeInfoChangeHandler CurrentModeChange;
    }
}