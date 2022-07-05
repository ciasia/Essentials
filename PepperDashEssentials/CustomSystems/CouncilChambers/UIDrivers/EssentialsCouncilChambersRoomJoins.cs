using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace CI.Essentials.CouncilChambers
{

    public class CoP_Joins
    {
        public static ushort PRESS_IDX = 0; // the 1st item in a join array is the press join
        public static ushort VIS_IDX = 1;   // the 2nd item in a join array is the visibility join
        public static ushort EN_IDX = 2;   // the 3rd item in a join array is the enable join
    }

    public class CoP_DigJoins
    {
        public const uint PowerPress = 16001;

        public const ushort HARD_BTN_PWR = 1;
        public const ushort HARD_BTN_HOME = 2;
        public const ushort HARD_BTN_LIGHTS = 3;
        public const ushort HARD_BTN_UP = 4;
        public const ushort HARD_BTN_DOWN = 5;


        public static ushort SUB_ONLINE = 11;
        public static ushort[] SUB_HOME = { 12, 13, 14 }; // Main, Gallery, Dining 
        public static ushort SUB_LOCKOUT = 15;
        public static ushort SUB_MICS = 16;
        public static ushort SUB_MODES = 17;
        public static ushort[] SUB_MUSIC = { 18, 19 }; // BGM, room
        public static ushort SUB_MUSIC_SOURCES = 19;
        public static ushort SUB_LIGHTS = 20;
        public static ushort[] SUB_PIN = { 21, 22 }; // enter, edit
        public static ushort SUB_SCHEDULE = 23;
        public static ushort SUB_STREAMING = 24;
        public static ushort SUB_DTV = 25;
        public static ushort SUB_VIDEO_MATRIX = 26;
        public static ushort SUB_CONFIRM = 27;
        public static ushort[] SUB_HELP = { 28, 29 }; // request, info
        public static ushort SUB_NOTICE = 30;
        public static ushort SUB_COUNTDOWN = 31;
        public static ushort SUB_YES_NO = 32;
        public static ushort SUB_OPERATOR = 33;

        public static ushort[] SUB_TOP_BAR = { 41, 42, 43 }; // Main, Gallery, Dining
        public static ushort[] SUB_BTM_BAR = { 46, 47, 48 }; // Operator, Scribe, Dining

        public static ushort[] POWER = { 101, 102 }; // press, visible, enabled
        public static ushort[] HELP = { 104, 105 };
        public static ushort[] LIGHTS = { 107, 108 };
        public static ushort[] MUSIC = { 110, 111 };
        public static ushort[] MICS = { 113, 114 };
        public static ushort[] AUDIO = { 116, 117 };
        public static ushort[] HOME = { 119, 120 };

        public static ushort[] USER = { 122, 123 };
        public static ushort[] STREAM = { 125, 126 };
        public static ushort[] RECORD = { 128, 129 };
        public static ushort[] MODE = { 131, 132 };
        public static ushort[] COMBINE = { 133, 134 };
        public static ushort[] CONFIDENTIAL = { 136, 137 };

        public static ushort[] LOGO = { 138, 139 };

        public static ushort VOL_MUTE = 141;
        public static ushort CONFIRM_YES = 142;
        public static ushort CONFIRM_NO = 143;
        public static ushort COUNTDOWN_VIS = 144;

        public static ushort VIDEO_MATRIX = 151;
        public static ushort SCHEDULE = 152;

        public static ushort MATRIX_AUDIO = 161;
        public static ushort MATRIX_VIDEO = 162;
        public static ushort MATRIX_ENTER = 163;
        public static ushort MATRIX_CANCEL = 164;
    }
    public class CoP_AnaJoins
    {
        public static ushort VOL_PROG = 1;
        public static ushort COUNTDOWN_BAR = 521;
    }
    public class CoP_SerJoins
    {
        public static ushort ROOM_NAME = 1;
        public static ushort ROOM_MODE = 2;
        public static ushort CONFIRM_TXT = 4;
    }
    public class CoP_SmartJoins
    {
        public const ushort AUTH_KEYPAD = 1;
        public const ushort AUDIO_SOURCES = 2;
        public const ushort MODES = 4;
        public const ushort OPERTOR_MENU = 5;
        //moved to CI.Essentials.UI
        //public const ushort faderSrl = 4200;
        //public const ushort vidMatrixInputsSrl = 4201;
        //public const ushort vidMatrixOutputsSrl = 4202;
    }

}