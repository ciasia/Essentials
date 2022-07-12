using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace CI.Essentials.Modes
{
    public class ModesController : IHasCurrentModeInfoChange, IModeControls, IKeyed
    {
        string IKeyed.Key { get { return "ModesController"; } }

        public IModesPropertiesConfig config { get; private set; }

        //public ModeSingleControlManager SystemModesControl { get; set; }
        public event EventHandler<ModeChangeEventArgs> ModeChange;

        public Dictionary<string, ModeSingleControlManager> ModesControlList { get; set; }
        public event EventHandler<KeyedModeChangeEventArgs> ModesDeviceListChange;


        public ModeDevice _CurrentMode { get; private set; }

        //public string CurrentModeInfoKey { get; set; }
        //public ModeListItem CurrentModeInfo
        //{
        //    get
        //    {
        //        return _CurrentModeInfo;
        //    }
        //    set
        //    {
        //        if (value == _CurrentModeInfo) return;

        //        var handler = CurrentModeChange;

        //        if (handler != null)
        //            handler(_CurrentModeInfo, ChangeType.WillChange);

        //        _CurrentModeInfo = value;

        //        if (handler != null)
        //            handler(_CurrentModeInfo, ChangeType.DidChange);
        //    }
        //}
        //ModeListItem _CurrentModeInfo;

        public ModesController(IModesPropertiesConfig config)
        {
            this.config = config;
            //default_device = DeviceManager.GetDeviceForKey(config.DefaultModeKey);

            //SystemModesControl = new ModeSingleControlManager(config.DefaultModeKey, default_device as IBasicModeControls);
            //MasterVolumeControl.CurrentDeviceChange += new EventHandler<KeyedVolumeDeviceChangeEventArgs>(MasterFader_CurrentDeviceChange);
            //Debug.Console(1, default_device, "Added MasterVolumeControl");

            ModesControlList = new Dictionary<string, ModeSingleControlManager>();
            foreach (var d in config.ModeList)
            {
                var name_ = d.Value.Name;
                var key_ = d.Value.modeKey;
                var dev_ = DeviceManager.GetDeviceForKey(key_);
                if (dev_ == null)
                {
                    dev_ = new ModeDevice(name_, key_);
                    DeviceManager.AddDevice(dev_);
                }
                var control_ = new ModeSingleControlManager(key_, dev_ as IBasicModeControls);
                ModesControlList.Add(key_, control_);
                control_.CurrentDeviceChange += new EventHandler<KeyedModeChangeEventArgs>(control__CurrentDeviceChange);
                Debug.Console(1, dev_, "Added to ModeControlList");
                //Debug.Console(1, this, "ModeControlList[{1}] {2}", key_, "CurrentControl".IsNullString(VolumeControlList[key_].CurrentControl));
            }
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                Debug.Console(1, this, "InitializeRoom");
                foreach (var v in ModesControlList)
                    v.Value.Initialize();
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Error Initializing Room: {0}", e);
            }
        }

        void control__CurrentDeviceChange(object sender, KeyedModeChangeEventArgs e)
        {
            Debug.Console(1, this, "ModeControlList_CurrentDeviceChange {0}", e.Key);
            //var dev_ = DeviceManager.GetDeviceForKey(e.Key);
        }

        #region IHasCurrentModeInfoChange Members

        /// <summary>
        /// The ModeListItem last run - containing names and icons 
        /// </summary>
        public ModeListItem CurrentModeInfo
        {
            get { return _CurrentModeInfo; }
            set
            {
                Debug.Console(0, this, "Setting CurrentModeInfo: {0}", value.Name);
                if (value == _CurrentModeInfo) return;

                var handler = CurrentModeChange;
                // remove from in-use tracker, if so equipped
                if (_CurrentModeInfo != null && _CurrentModeInfo.ModeDevice is IInUseTracking)
                    (_CurrentModeInfo.ModeDevice as IInUseTracking).InUseTracker.RemoveUser(this, "control");

                if (handler != null)
                    handler(_CurrentModeInfo, ChangeType.WillChange);

                _CurrentModeInfo = value;

                // add to in-use tracking
                if (_CurrentModeInfo != null && _CurrentModeInfo.ModeDevice is IInUseTracking)
                    (_CurrentModeInfo.ModeDevice as IInUseTracking).InUseTracker.AddUser(this, "control");
                if (handler != null)
                    handler(_CurrentModeInfo, ChangeType.DidChange);
            }
        }
        ModeListItem _CurrentModeInfo;

        public event ModeInfoChangeHandler CurrentModeChange;

        /// <summary>
        /// Sets the ModeListKey property to the passed in value or the default if no value passed in
        /// </summary>
        /// <param name="sourceListKey"></param>
        protected void SetModeListKey(string listKey)
        {
            if (!string.IsNullOrEmpty(listKey))
            {
                ModeListKey = listKey;
            }
            else
            {
                listKey = _defaultModeListKey;
            }
        }

        private void SetModeListKey()
        {
            if (!string.IsNullOrEmpty(config.ModeListKey))
            {
                SetModeListKey(config.ModeListKey);
            }
            else
            {
                Debug.Console(1, this, "ModeController SetModeListKey is null");
                //SetModeListKey(default_device.Key);
            }
        }

        /// <summary>
        /// The config name of the mode list
        /// </summary>
        /// 
        protected string _ModeListKey;
        public string ModeListKey
        {
            get
            {
                return _ModeListKey;
            }
            private set
            {
                if (value != _ModeListKey)
                {
                    _ModeListKey = value;
                }
            }
        }

        protected const string _defaultModeListKey = "default";

        #endregion
    }
}