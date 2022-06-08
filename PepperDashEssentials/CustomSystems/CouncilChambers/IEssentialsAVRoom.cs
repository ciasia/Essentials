using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace PepperDashEssentials.CustomSystems
{
    public interface IEssentialsAVRoom : IEssentialsRoom//, 
        //IHasCurrentSourceInfoChange,
        //IPrivacy, 
        //IHasCurrentVolumeControls, 
        //IRunRouteAction, 
        //IHasDefaultDisplay
    {
        EssentialsAvRoomPropertiesConfig PropertiesConfig { get; }

        //bool ExcludeFromGlobalFunctions { get; }

        //void RunRouteAction(string routeKey);

        //IHasScheduleAwareness ScheduleSource { get; }
    }
}