using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using CI.Essentials.CouncilChambers;
using CI.Essentials.Utilities;
using CI.Essentials.UI;
using PepperDash.Essentials.Core.SmartObjects;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CI.Essentials.Modes
{
    /// <summary>
    /// The handler type for a Room's ModeInfoChange
    /// </summary>
    public delegate void ModeInfoChangeHandler(ModeListItem info, ChangeType type);

    //public class SmartObjectModeList : SmartObjectDynamicList
    //{
    //    public uint StatusSigOffset { get; private set; }
    //    List<SmartObjectModeListItem> Items;

    //    public SmartObjectModeList(SmartObject so, uint nameSigOffset, uint statusSigOffset)
    //        : base(so, true, nameSigOffset)
    //    {
    //        StatusSigOffset = statusSigOffset;
    //        Items = new List<SmartObjectModeListItem>();
    //    }

    //    public void AddItem(SmartObjectModeListItem item)
    //    {
    //        Items.Add(item);
    //    }

    //    public void SetItemStatusText(uint index, string text)
    //    {
    //        if (index > MaxCount) return;
    //        // The list item template defines CIPS tags that refer to standard joins
    //        (SmartObject.Device as BasicTriList).StringInput[StatusSigOffset + index].StringValue = text;
    //    }

    //    /// <summary>
    //    /// Sets feedback for the given room
    //    /// </summary>
    //    public void SetFeedbackForItem(SmartObjectModeListItem item)
    //    {
    //        var itemToSet = Items.FirstOrDefault(i => i.Mode == item);
    //        if (itemToSet != null)
    //            SetFeedback(itemToSet.Index, true);
    //    }
    //}

    //public class SmartObjectModeListItem
    //{
    //    public SmartObjectModeListItem Mode { get; private set; }
    //    SmartObjectModeList Parent;
    //    public uint Index { get; private set; }

    //    public SmartObjectModeListItem(SmartObjectModeListItem mode, uint index, SmartObjectModeList parent, 
    //        Action<bool> buttonAction)
    //    {
    //        Mode = mode;
    //        Parent = parent;
    //        Index = index;
    //        if (mode == null) return;

    //        // Set "now" states
    //        //parent.SetItemMainText(index, room.Name);
    //        UpdateItem(mode.CurrentModeInfo);
    //        // Watch for later changes
    //        mode.CurrentModeChange += new ModeInfoChangeHandler(room_CurrentModeChange);
    //        parent.SetItemButtonAction(index, buttonAction);
    //    }

    //    void room_CurrentModeChange(ModeListItem sender, ChangeType type)
    //    {
    //        UpdateItem(sender);
    //    }

    //    /// <summary>
    //    /// Helper to handle events and startup syncing with room's current mode
    //    /// </summary>
    //    /// <param name="info"></param>
    //    void UpdateItem(ModeListItem info)
    //    {
    //        if (info == null)
    //        {
    //            Parent.SetItemStatusText(Index, "");
    //            Parent.SetItemIcon(Index, "Blank");
    //        }
    //        else
    //        {
    //            Parent.SetItemStatusText(Index, info.Name);
    //            //Parent.SetItemIcon(Index, info.icon);
    //        }
    //    }
    //}


    public class SystemModeFunctionDriver : PanelDriverBase, IEssentialsConnectableRoomDriver, IKeyed
    {
        string IKeyed.Key { get { return "ModesUIDriver"; } }
        private IEssentialsModalRoom _currentRoom;

        private SmartObject ModesSO;
        SmartObjectDynamicList ModesListSO;
        //List<SmartObjectModeListItem> Items;
        //private uint _modesListCount;

        public SystemModeFunctionDriver(PanelDriverBase parent)
            : base(parent.TriList)
        {
            Debug.Console(1, this, "Loading");
            ModesSO = TriList.SmartObjects[SmartJoins.systemModesList];
            Debug.Console(2, this, "ModesSO".IsNullString(ModesSO));
            ModesListSO = new SmartObjectDynamicList(ModesSO, true, 0);
            Debug.Console(2, this, "ModesSO".IsNullString(ModesSO));
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Show()
        {
            Debug.Console(1, this, "Show");
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void DisconnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "DisconnectCurrentRoom");
            try
            {
                _currentRoom = (IEssentialsModalRoom)room;

                if (_currentRoom != null)
                {
                    // Disconnect current room
                    //_currentRoom.audio.MasterVolumeDeviceChange -= this.CurrentRoom_CurrentMasterAudioDeviceChange;
                    //_currentRoom.modes.CurrentModeChange -= 
                    ClearDeviceConnections();
                }
                else
                    Debug.Console(1, this, "_currentRoom".IsNullString(_currentRoom));
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "DisconnectCurrentRoom ERROR: {0}", e.Message);
            }

        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        public void ClearDeviceConnections()
        {
            Debug.Console(1, this, "ClearDeviceConnections");
            ModesListSO.ClearActions();
            ModesListSO.ClearFeedbacks();
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void ConnectCurrentRoom(IEssentialsRoom room)
        {
            Debug.Console(1, this, "ConnectCurrentRoom");
            try
            {
                _currentRoom = (IEssentialsModalRoom)room;
                //Debug.Console(1, this, "_CurrentRoom".IsNullString(_currentRoom));

                if (_currentRoom != null)
                {
                    Debug.Console(1, this, "subscribing to CurrentVolumeDeviceChange");
                    RefreshDeviceConnections();
                }
                else
                    Debug.Console(1, this, "_currentRoom".IsNullString(_currentRoom));
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "ConnectCurrentRoom ERROR: {0}", e.Message);
            }
        }

        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        public void RefreshDeviceConnections()
        {
            Debug.Console(1, this, "RefreshDeviceConnections");
            if (_currentRoom != null)
            {
                Debug.Console(1, this, "CurrentModeControls connect buttons");
                //ModesSO.SigChange += new SmartObjectSigChangeEventHandler(ModesSO_SigChange);
                RefreshModeList();
            }
        }


        /// <summary>
        /// Mode List - connect the joins to funcs
        /// </summary>
        void RefreshModeList()
        {
            try
            {
                Debug.Console(1, this, "RefreshModeList");
                var config = _currentRoom.modes.config.ModeList;
                var modeList = config.OrderBy(kv => kv.Value.Order);

                // Setup sources list			
                ModesListSO.ClearActions();
                ModesListSO.ClearFeedbacks();
                uint i = 1; // counter for UI list
                foreach (var kvp in modeList)
                {
                    var modeConfig = kvp.Value;
                    var key_ = modeConfig.modeKey;
                    Debug.Console(1, this, "RefreshModeList {0}, {1} {2}", key_, modeConfig.Name, modeConfig.IncludeInModeList);
                    Debug.Console(2, this, "ModesListSO".IsNullString(ModesListSO));
                    // Skip sources marked as not included, and filter list of non-sharable sources when in call
                    // or on share screen
                    if (!modeConfig.IncludeInModeList)
                    {
                        Debug.Console(1, this, "Skipping {0}", modeConfig.Name);
                        continue;
                    }
                    if (_currentRoom.modes.ModesControlList.ContainsKey(key_))
                    {
                        var dev_ = _currentRoom.modes.ModesControlList[key_].CurrentControl;
                        Debug.Console(2, this, "RefreshModeList - ModesControlList[{0}].{1}", key_, "CurrentControl".IsNullString(dev_));
                        if (dev_ != null) // connect buttons
                        {
                            var fbDev_ = dev_ as IBasicModeWithFeedback;
                            Debug.Console(2, this, "RefreshModeList, {0} {1}", key_, "CurrentControl".IsNullString(fbDev_));
                            var level_ = new DynamicListItem(i, ModesListSO, modeConfig,
                                  v => { fbDev_.SetVolume(v); },
                                  b => { if (!b) fbDev_.MuteToggle(); },
                                  u => { fbDev_.VolumeUp(u); },
                                  d => { fbDev_.VolumeDown(d); }
                                  );
                            Debug.Console(2, this, "RefreshModeList, {0} {1}", key_, "level_".IsNullString(level_));
                            ModesListSO.AddItem(level_); // add to the SRL
                            Debug.Console(2, this, "RefreshModeList, {0} VolumeSrl added level", key_);
                            level_.RegisterForLevelChange(_currentRoom.audio);
                            Debug.Console(2, this, "RefreshModeList, {0} RegisterForLevelChange", key_);
                            string visibleKey_ = String.Format("Item {0} Visible", i);
                            //Debug.Console(2, this, "RefreshVolumeList Setting SmartObject {1}", classname, joinKey_);
                            ModesSO.BooleanInput[visibleKey_].BoolValue = true;

                            if (fbDev_ == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                                ModesListSO.UShortInputSig(i, 1).UShortValue = (ushort)0;
                            else
                            {
                                // feedbacks
                                fbDev_.MuteFeedback.LinkInputSig(ModesListSO.BoolInputSig(i, 1));
                                fbDev_.VolumeLevelFeedback.LinkInputSig(ModesListSO.UShortInputSig(i, 1));
                            }
                        }
                        else
                            Debug.Console(1, this, "CurrentVolumeControls".IsNullString(key_));
                    }
                    else
                        Debug.Console(1, this, "VolumeControlList.ContainsKey({0}) == false", key_);
                    i++;
                }
                _volumeListCount = (i - 1);
                ModesListSO.Count = (ushort)_volumeListCount;
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "RefreshModeList ERROR: {0}", e.Message);
            }
        }


        void ModesSO_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            Debug.Console(1, this, "ModesSO_SigChange");
            //args.Sig.UserObject;
        }

        void ModePress()
        {

        }
    }
}