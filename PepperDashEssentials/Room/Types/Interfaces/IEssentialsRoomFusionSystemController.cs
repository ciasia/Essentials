using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDashEssentials.Room.Types.Interfaces
{
    // this is currently a filler so that we can look for classes that implment it
    public interface IEssentialsRoomFusionSystemController : IKeyName, IKeyed
    {
        void EssentialsRoomFusionSystemController(IEssentialsRoom room, uint ipId, string joinMapKey);
    }
}