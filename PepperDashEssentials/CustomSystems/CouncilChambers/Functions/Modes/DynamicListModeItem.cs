using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;

using PepperDash.Essentials.Core;
using PepperDash.Core;
using CI.Essentials.Modes;
using CI.Essentials.UI;
using PepperDash.Essentials.Core.SmartObjects;

namespace CI.Essentials.Levels
{
    /// <summary>
    /// The handler type for a Room's LevelInfoChange
    /// </summary>
    public delegate void ModenfoChangeHandler(ModeListItem info, ChangeType type);

    public class DynamicListModeItem: DynamicListItem
    {
        public ModeListItem ModeItem { get; private set; }

        private IHasCurrentLevelInfoChange _room;

        public DynamicListModeItem(uint index, SmartObjectDynamicList owner,
            LevelListItem modeItem, Action<bool> selectAction)
            : base(index, owner)
        {
            try
            {
                ModeItem = modeItem;
                owner.GetUShortOutputSig(index, 1).UserObject = new Action<ushort>(levelAction);
                owner.GetBoolFeedbackSig(index, 1).UserObject = new Action<bool>(toggleAction);
                owner.StringInputSig(index, 1).StringValue = levelItem.Label;
            }
            catch (Exception e)
            {
                Debug.Console(1, "DynamicListModeItem[{0}] ERROR: {1}", index, e.Message);
            }
        }

        public DynamicListModeItem(uint index, SubpageReferenceList owner,
            LevelListItem levelItem, Action<ushort> levelAction, Action<bool> toggleAction,
            Action<bool> levelUpAction, Action<bool> levelDownAction)
            : base(index, owner)
        {
            try
            {
                LevelItem = levelItem;
                owner.GetUShortOutputSig(index, 1).UserObject = new Action<ushort>(levelAction);
                owner.GetBoolFeedbackSig(index, 1).UserObject = new Action<bool>(toggleAction);
                owner.StringInputSig(index, 1).StringValue = levelItem.Label;

                owner.GetBoolFeedbackSig(index, 2).UserObject = new Action<bool>(levelUpAction);
                owner.GetBoolFeedbackSig(index, 3).UserObject = new Action<bool>(levelDownAction);
            }
            catch (Exception e)
            {
                Debug.Console(1, "DynamicListModeItem[{0}] ERROR: {1}", index, e.Message);
            }

        }

        public void RegisterForLevelChange(IHasCurrentLevelInfoChange room)
        {
            _room = room;
            room.CurrentLevelChange -= room_CurrentLevelInfoChange;
            room.CurrentLevelChange += room_CurrentLevelInfoChange;
        }

        void room_CurrentLevelInfoChange(LevelListItem info, ChangeType type)
        {
            if (type == ChangeType.WillChange && info == LevelItem)
                ClearFeedback();
            else if (type == ChangeType.DidChange && info == LevelItem)
                SetFeedback();
        }

        /// <summary>
        /// Called by SRL to release all referenced objects
        /// </summary>
        public override void Clear()
        {
            Owner.BoolInputSig(Index, 1).UserObject = null;
            Owner.StringInputSig(Index, 1).StringValue = "";

            if (_room != null)
                _room.CurrentLevelChange -= room_CurrentLevelInfoChange;
        }

        /// <summary>
        /// Sets the selected feedback on the button
        /// </summary>
        public void SetFeedback()
        {
            Owner.BoolInputSig(Index, 1).BoolValue = true;
        }

        /// <summary>
        /// Clears the selected feedback on the button
        /// </summary>
        public void ClearFeedback()
        {
            Owner.BoolInputSig(Index, 1).BoolValue = false;
        }
    }
}