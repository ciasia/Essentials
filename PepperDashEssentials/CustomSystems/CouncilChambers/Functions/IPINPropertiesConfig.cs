using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Room.Config;
using CI.Essentials.Levels;

namespace CI.Essentials.PIN
{
    public interface IPINPropertiesConfig
    {
        /// <summary>
        /// The key of the default password for the room
        /// TODO [ ] - make it encrypted
        /// </summary>
        [JsonProperty("password")]
        string Password { get; set; }
    }
}