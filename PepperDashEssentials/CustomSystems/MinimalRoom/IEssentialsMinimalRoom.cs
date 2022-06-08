using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace PepperDash.Essentials.MinimalRoom
{
    public interface IEssentialsMinimalRoom : IEssentialsRoom
    {
        EssentialsRoomPropertiesConfig PropertiesConfig { get; }
    }
}