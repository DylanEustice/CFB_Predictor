using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Diagnostics;

namespace CFB_Predictor
{
    public partial class Program
    {
        // CFB data constants
        public const int TEAM_CODE = 0;
        public const int GAME_CODE = 1;
        public const int RUSH_ATT = 2;
        public const int RUSH_YARD = 3;
        public const int RUSH_TD = 4;
        public const int PASS_ATT = 5;
        public const int PASS_COMP = 6;
        public const int PASS_YARD = 7;
        public const int PASS_TD = 8;
        public const int PASS_INT = 9;
        public const int PASS_CONV = 10;
        public const int KICKOFF_RET = 11;
        public const int KICKOFF_RET_YARD = 12;
        public const int KICKOFF_RET_TD = 13;
        public const int PUNT_RET = 14;
        public const int PUNT_RET_YARD = 15;
        public const int PUNT_RET_TD = 16;
        public const int FUM_RET = 17;
        public const int FUM_RET_YARD = 18;
        public const int FUM_RET_TD = 19;
        public const int INT_RET = 20;
        public const int INT_RET_YARD = 21;
        public const int INT_RET_TD = 22;
        public const int MISC_RET = 23;
        public const int MISC_RET_YARD = 24;
        public const int MISC_RET_TD = 25;
        public const int FIELD_GOAL_ATT = 26;
        public const int FIELD_GOAL_MADE = 27;
        public const int OFF_XP_KICK_ATT = 28;
        public const int OFF_XP_KICK_MADE = 29;
        public const int OFF_2XP_ATT = 30;
        public const int OFF_2XP_MADE = 31;
        public const int DEF_2XP_ATT = 32;
        public const int DEF_2XP_MADE = 33;
        public const int SAFETY = 34;
        public const int POINTS = 35;
        public const int PUNT = 36;
        public const int PUNT_YARD = 37;
        public const int KICKOFF = 38;
        public const int KICKOFF_YARD = 39;
        public const int KICKOFF_TOUCHBACK = 40;
        public const int KICKOFF_OB = 41;
        public const int KICKOFF_ONSIDES = 42;
        public const int FUMBLE = 43;
        public const int FUMBLE_LOST = 44;
        public const int TACKLE_SOLO = 45;
        public const int TACKLE_AST = 46;
        public const int TACKLE_FOR_LOSS = 47;
        public const int TACKLE_FOR_LOSS_YARD = 48;
        public const int SACK = 49;
        public const int SACK_YARD = 50;
        public const int QB_HURRY = 51;
        public const int FUMBLE_FORCED = 52;
        public const int PASS_BROKEN_UP = 53;
        public const int KICK_PUNT_BLOCKED = 54;
        public const int FIRST_DOWN_RUSH = 55;
        public const int FIRST_DOWN_PASS = 56;
        public const int FIRST_DOWN_PENALTY = 57;
        public const int TIME_OF_POS = 58;
        public const int PENALTY = 59;
        public const int PENALTY_YARD = 60;
        public const int THIRD_DOWN_ATT = 61;
        public const int THIRD_DOWN_CONV = 62;
        public const int FOURTH_DOWN_ATT = 63;
        public const int FOURTH_DOWN_CONV = 64;
        public const int RED_ZONE_ATT = 65;
        public const int RED_ZONE_TD = 66;
        public const int RED_ZONE_FG = 67;

        public const int CONF = 68;                     // Use game stat
        public const int IS_HOME = 69;                  // Use game stat
        public const int TOTAL_YARDS = 70;
        public const int TO_AGAINST = 71;
        public const int TO_FOR = 72;
        public const int TO_NET = 73;
        public const int YARD_RATIO = 74;
        public const int PASSING_EFF = 75;
        public const int OOC_PYTHAG = 76;               // Use game stat
        public const int OOC_PYTHAG_RATIO = 77;        // DISCONTINUED // Use game stat
        public const int PYTHAG_EXPECT = 78;           // Use game stat
        public const int HV_PY_EXPECT = 79;             // Use game stat
        public const int HOME_TIMES_HVPE = 80;          // Use game stat
        public const int BIAS = 81;                     // Use game stat
        public const int ADJ_RUSH_AVG = 82;
        public const int ADJ_PASS_AVG = 83;
        public const int XTRA_DATA_PTS = 16;
        public const int N_DATA_PTS = 68 + XTRA_DATA_PTS;

        public readonly static int[] USE_GAME_STAT = { CONF, IS_HOME, OOC_PYTHAG, OOC_PYTHAG_RATIO, PYTHAG_EXPECT, HV_PY_EXPECT, HOME_TIMES_HVPE, YARD_RATIO, BIAS };

        // Algorithm constants
        public const int HOME = 0;
        public const int VISITOR = 1;
        public const int RAND = 2;
        public const int DIF = 0;
        public const int SIM_AVG = 1;
        public const int PTS_AVG = 2;
        public const int TRAIN = 0;
        public const int DUMB_ACC = 1;
        public const int SIM_DATES = 2;

        // 2013 weeks
        public readonly static int[][] WEEKS_2013 = 
        { 
            new int[] { 20130827, 20130902 }, // week 1  [0]
            new int[] { 20130903, 20130909 }, //      2  [1]
            new int[] { 20130910, 20130916 }, //      3  [2]
            new int[] { 20130917, 20130923 }, //      4  [3]
            new int[] { 20130924, 20130930 }, //      5  [4]
            new int[] { 20131001, 20131007 }, //      6  [5]
            new int[] { 20131008, 20131014 }, //      7  [6]
            new int[] { 20131015, 20131021 }, //      8  [7]
            new int[] { 20131022, 20131028 }, //      9  [8]
            new int[] { 20131029, 20131104 }, //      10 [9]
            new int[] { 20131105, 20131111 }, //      11 [10]
            new int[] { 20131112, 20131118 }, //      12 [11]
            new int[] { 20131119, 20131125 }, //      13 [12]
            new int[] { 20131126, 20131202 }, //      14 [13]
            new int[] { 20131203, 20131209 }, //      15 [14]
            new int[] { 20131210, 20131216 }, //      16 [15]
            new int[] { 20131221, 20140106 }, // bowls   [16]
            new int[] { 20140107, 20140114 }, // end     [17]
        };
    }
}