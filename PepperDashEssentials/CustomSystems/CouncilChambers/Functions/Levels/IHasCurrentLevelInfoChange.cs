using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace CI.Essentials.Levels
{
    /// <summary>
    /// For rooms with a single presentation source, change event
    /// </summary>
    public interface IHasCurrentLevelInfoChange
    {
        //string CurrentLevelInfoKey { get; set; }
        LevelListItem CurrentLevelInfo { get; set; }
        event LevelInfoChangeHandler CurrentLevelChange;
    }
}