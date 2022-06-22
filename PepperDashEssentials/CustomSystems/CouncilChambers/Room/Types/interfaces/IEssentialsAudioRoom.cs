using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using CI.Essentials.Audio;

namespace CI.Essentials.Audio
{
    /// <summary>
    /// used for AudioPanelFunctionsDriver to pass the audio properties to a room
    /// </summary>
    public interface IEssentialsAudioRoom : IEssentialsRoom//, IAudioControls
    {
        AudioController audio { get; }
    }
}