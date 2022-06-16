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

        /// <summary>
        /// Put VolumeList in the room config so it only affects room code
        /// if we put it in the base config like sourceList then we'd have to modify
        ///  EssentialsConfig which affects all room config types
        /// </summary>
        [JsonProperty("volumeList")]
        public Dictionary<string, LevelListItem> VolumeList { get; set; }

        [JsonProperty("volumeListKey")]
        public string VolumeListKey { get; set; }
    }
}