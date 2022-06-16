using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DspRoom
{

	/// <summary>
	/// Represents an item in a level list - can be deserialized into.
	/// </summary>
	public class LevelListItem
	{
		[JsonProperty("levelKey")]
		public string LevelKey { get; set; }

		/// <summary>
		/// Returns the source Device for this, if it exists in DeviceManager
		/// </summary>
		[JsonIgnore]
		public Device LevelDevice
		{
			get
			{
				if (_LevelDevice == null)
					_LevelDevice = DeviceManager.GetDeviceForKey(LevelKey) as Device;
				return _LevelDevice;
			}
		}
		Device _LevelDevice;

		/// <summary>
		/// A name that will override the device's name on the UI
		/// </summary>
		[JsonProperty("label")]
		public string Label { get; set; }

        /// <summary>
        /// Used to specify the order of the items in the  list when displayed
        /// </summary>
        [JsonProperty("order")]
        public int Order { get; set; }

        /// <summary>
        /// Used to specify the order of the items in the  list when displayed
        /// </summary>
        [JsonProperty("includeInVolumeList")]
        public bool IncludeInVolumeList { get; set; }

        public LevelListItem()
		{
		}

        
	}
}