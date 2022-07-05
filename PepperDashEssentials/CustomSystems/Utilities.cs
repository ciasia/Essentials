using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Crestron.SimplSharp;

namespace CI.Essentials.Utilities
{
    public static class StringExtensions
    {
        public static string IsNullString(this String name, object o)
        {
            return System.String.Format("{0} {1}= null", name, o == null ? "=" : "?");
        }           
    }
}