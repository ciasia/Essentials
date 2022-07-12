using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using CI.Essentials.Audio;
using CI.Essentials.Modes;

namespace CI.Essentials.CouncilChambers
{
    /// <summary>
    /// Describes the basic functionality of an EssentialsRoom
    /// </summary>
    //public interface IEssentialsCouncilChambers
    //    : IKeyName//, IReconfigurableDevice, IRunDefaultPresentRoute, IEnvironmentalControls
    //{
    //}
    public interface IEssentialsCouncilChambers : IEssentialsRoom, IEssentialsAudioRoom, IEssentialsModalRoom //, IAudioControls
    {
        EssentialsCouncilChambersPropertiesConfig PropertiesConfig { get; }
        //AudioController audio { get; }
    }
}