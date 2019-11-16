﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Room.MobileControl
{
	/// <summary>
	/// Represents a room whose configuration is derived from runtime data,
	/// perhaps from another program, and that the data may not be fully
	/// available at startup.
	/// </summary>
	public interface IDelayedConfiguration
	{
		event EventHandler<EventArgs> ConfigurationIsReady;
	}
}

