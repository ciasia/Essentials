using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Room.Config;
using CI.Essentials.Levels;

namespace CI.Essentials.Modes
{
    public interface IModesPropertiesConfig
    {
        /// <summary>
        /// The key of the default Mode device for the main list
        /// </summary>
        [JsonProperty("defaultModeKey")]
        string DefaultModeKey { get; set; }

        /// <summary>
        /// Put ModeList in the room config so it only affects room code
        /// if we put it in the base config like sourceList then we'd have to modify
        ///  EssentialsConfig which affects all room config types
        /// 
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "modeeList": { 
        ///                "mode-01": {
        ///                    "order": 1,
        ///                    "label": "Mode 1",
        ///                    "includeInModeList": true
        ///                }, 
        ///                "mode-02": {
        ///                    "order": 2,
        ///                    "label": "Mode 2",
        ///                    "includeInModeList": true
        ///                }
        ///            }
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("modeList")]
        Dictionary<string, ModeListItem> ModeList { get; set; }

        /// <summary>
        /// Not sure where this is used.
        /// example config...
        ///"rooms": [
        ///    {
        ///    "key": "room1",
        ///        "properties": {
        ///            "modeListKey": "room1"
        ///        }
        ///    }
        ///]
        /// </summary>
        [JsonProperty("modeListKey")]
        string ModeListKey { get; set; }
    }
}