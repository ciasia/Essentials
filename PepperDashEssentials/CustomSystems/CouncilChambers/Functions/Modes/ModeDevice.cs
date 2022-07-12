using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace CI.Essentials.Modes
{
    public class ModeDevice: IBasicModeControls, IKeyed
    {
        public string Key { get; private set; }
        public string Name;

        public ModeDevice(string name, string key)
        {
            Key = key;
            Name = name;
        }

        #region IBasicModeControls Members

        public void ModeChange(bool pressRelease)
        {
            Debug.Console(0, this, "ModeChange {0}", pressRelease);
        }

        #endregion
    }
}