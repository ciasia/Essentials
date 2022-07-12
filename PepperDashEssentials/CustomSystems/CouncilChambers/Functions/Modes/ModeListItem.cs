using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// Represents an item in a mode list - can be deserialized into.
    /// </summary>
    public class ModeListItem
    {
        [JsonProperty("modeKey")]
        public string modeKey { get; set; }

        /// <summary>
        /// Returns the Device for this, if it exists in DeviceManager
        /// </summary>
        [JsonIgnore]
        public Device ModeDevice
        {
            get
            {
                if (_ModeDevice == null)
                    _ModeDevice = DeviceManager.GetDeviceForKey(modeKey) as Device;
                return _ModeDevice;
            }
        }
        Device _ModeDevice;

        /// <summary>
        /// A name that will override the source's name on the UI
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Specifies and icon for the source list item
        /// </summary>
        //[JsonProperty("icon")]
        //public string Icon { get; set; }

        /// <summary>
        /// Indicates if the item should be included in the list
        /// </summary>
        [JsonProperty("includeInModeList")]
        public bool IncludeInModeList { get; set; }

        /// <summary>
        /// Used to specify the order of the items in the source list when displayed
        /// </summary>
        [JsonProperty("order")]
        public int Order { get; set; }

        /// <summary>
        /// A means to reference a list for this item, in the event that this mode has an item that can have functions routed to it
        /// </summary>
        //[JsonProperty("modeListKey")]
        //public string SourceListKey { get; set; }

        public ModeListItem()
        {
            //Icon = "Blank";
        }
    }
}