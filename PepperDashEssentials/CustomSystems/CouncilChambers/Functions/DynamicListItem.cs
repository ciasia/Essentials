using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using PepperDash.Essentials.Core.SmartObjects;

namespace CI.Essentials.UI
{
    public class DynamicListItem
    {
        /// <summary>
        /// The list that this lives in
        /// </summary>
        protected DynamicList Owner;
        protected uint Index;

        public DynamicListItem(uint index, SmartObjectDynamicList owner)
        {
            Index = index;
            Owner = owner;
        }

        /// <summary>
        /// Called by SRL to release all referenced objects
        /// </summary>
        public virtual void Clear()
        {
        }

        public virtual void Refresh() { }
    }
}