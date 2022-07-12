using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// For rooms with a mode, change event
    /// </summary>
    public interface IHasCurrentModeInfoChange
    {
        //string CurrentLevelInfoKey { get; set; }
        ModeListItem CurrentModeInfo { get; set; }
        event ModeInfoChangeHandler CurrentModeChange;
    }
}