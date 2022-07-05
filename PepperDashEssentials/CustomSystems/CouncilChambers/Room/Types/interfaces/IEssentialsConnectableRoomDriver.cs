using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace CI.Essentials
{
    public interface IEssentialsConnectableRoomDriver
    {
        void DisconnectCurrentRoom(IEssentialsRoom room);
        void ConnectCurrentRoom(IEssentialsRoom room);
    }
}