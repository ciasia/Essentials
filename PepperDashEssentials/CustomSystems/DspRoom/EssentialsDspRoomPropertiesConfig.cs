using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Essentials.Room.Config;

namespace PepperDash.Essentials.DspRoom
{
    public class EssentialsDspRoomPropertiesConfig : EssentialsRoomPropertiesConfig
    {
        /// <summary>
        /// The key of the default audio device
        /// </summary>
        [JsonProperty("defaultAudioKey")]
        public string DefaultAudioKey { get; set; }

        [JsonProperty("hasDsp")]
        public bool HasDsp { get; set; }
    }
}