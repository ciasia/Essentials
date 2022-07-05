using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;

using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace CI.Essentials.Levels
{
    /// <summary>
    /// The handler type for a Room's LevelInfoChange
    /// </summary>
    public delegate void LevelInfoChangeHandler(/*EssentialsRoomBase room,*/ LevelListItem info, ChangeType type);

    /// <summary>
    /// For rooms with a single presentation source, change event
    /// </summary>
    public interface IHasCurrentLevelInfoChange
    {
        //string CurrentLevelInfoKey { get; set; }
        LevelListItem CurrentLevelInfo { get; set; }
        event LevelInfoChangeHandler CurrentLevelChange;
    }

    public class SubpageReferenceListLevelItem : SubpageReferenceListItem
    {
        public LevelListItem LevelItem { get; private set; }

        private IHasCurrentLevelInfoChange _room;

        public SubpageReferenceListLevelItem(uint index, SubpageReferenceList owner,
            LevelListItem levelItem, Action<ushort> levelAction, Action<bool> toggleAction)
            : base(index, owner)
        {
            try
            {
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] levelItem{1}", index, levelItem == null ? " is NULL" : "");
                LevelItem = levelItem;
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] owner{1}", index, owner == null ? " is NULL" : "");
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] a-sig{1}", index, owner.GetUShortOutputSig(index, 1) == null ? " is NULL" : "");
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] levelAction{1}", index, levelAction == null ? " is NULL" : "");
                owner.GetUShortOutputSig(index, 1).UserObject = new Action<ushort>(levelAction);
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] d-sig{1}", index, owner.GetBoolFeedbackSig(index, 1) == null ? " is NULL" : "");
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] toggleAction{1}", index, toggleAction == null ? " is NULL" : "");
                owner.GetBoolFeedbackSig(index, 1).UserObject = new Action<bool>(toggleAction);
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] s-sig{1}", index, owner.StringInputSig(index, 1) == null ? " is NULL" : "");
                //Debug.Console(2, "SubpageReferenceListLevelItem[{0}] levelItem.Label{1}", index, String.IsNullOrEmpty(levelItem.Label) ? " is NULL" : ": levelItem.Label");
                owner.StringInputSig(index, 1).StringValue = levelItem.Label;
            }
            catch (Exception e)
            {
                Debug.Console(1, "SubpageReferenceListLevelItem[{0}] ERROR: {1}", index, e.Message);
            }
        }

        public SubpageReferenceListLevelItem(uint index, SubpageReferenceList owner,
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
                 Debug.Console(1, "SubpageReferenceListLevelItem[{0}] ERROR: {1}", index, e.Message);
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