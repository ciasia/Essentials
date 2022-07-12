using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Room.Config;
using CI.Essentials.Levels;
using CI.Essentials.Audio;
using CI.Essentials.PIN;
using CI.Essentials.Modes;

namespace CI.Essentials.CouncilChambers
{
    public class EssentialsCouncilChambersPropertiesConfig : EssentialsRoomPropertiesConfig, IAudioPropertiesConfig, IPINPropertiesConfig, IModesPropertiesConfig
    {        
        /// <summary>
        /// The key of the default audio device for the main volume fader
        /// </summary>
        [JsonProperty("defaultAudioKey")]
        public string DefaultAudioKey { get; set; }

        /// <summary>
        /// Put VolumeList in the room config so it only affects room code
        /// if we put it in the base config like sourceList then we'd have to modify
        ///  EssentialsConfig which affects all room config types
        /// 
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "volumeList": { 
        ///                "level-01": {
        ///                    "order": 1,
        ///                    "levelKey": "qsysdsp-1--VolLevelControl01",
        ///                    "label": "Volume",
        ///                    "includeInVolumeList": true
        ///                },
        ///                "level-02": {
        ///                    "order": 3,
        ///                    "levelKey": "qsysdsp-1--MicLevelControl01",
        ///                    "label": "Mic 1",
        ///                    "includeInVolumeList": true
        ///                }
        ///            }
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("volumeList")]
        public Dictionary<string, LevelListItem> VolumeList { get; set; }

        /// <summary>
        /// Not sure where this is used.
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "volumeListKey": "room1"
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("volumeListKey")]
        public string VolumeListKey { get; set; }

        /// <summary>
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "password": "1234"
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("modeList")]
        public Dictionary<string, ModeListItem> ModeList { get; set; }

        /// <summary>
        /// Not sure where this is used.
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "ModeListKey": "room1"
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("modeListKey")]
        public string ModeListKey { get; set; }
        
        /// <summary>
        /// The key of the default Mode device for the main Mode
        /// </summary>
        [JsonProperty("defaultModeKey")]
        public string DefaultModeKey { get; set; }

    }
}