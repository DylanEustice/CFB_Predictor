using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Game
    {
        // Game data
        public long GameCode;
        public int Date;
        // Visitor data
        public int VisitorCode;
        public Team VisitorTeam;
        public double[] VisitorData = new double[Program.N_DATA_PTS];
        // Home data
        public int HomeCode;
        public Team HomeTeam;
        public double[] HomeData = new double[Program.N_DATA_PTS];
        // Who won?
        public bool HomeWin = false;
        public bool VisitorWin = false;
        public bool Tie = false;

        //
        // Constructor
        public Game()
        {
            // Get information from game code
            GameCode = 0;
            VisitorCode = 0;
            HomeCode = 0;
            Date = 0;
        }
        public Game(double gameCode, double[][] allData, Team thisTeam)
        {
            // Get information from game code
            GameCode = Convert.ToInt64(gameCode);
            VisitorCode = (int)Math.Floor(GameCode / Math.Pow(10, 12));
            HomeCode = (int)(Math.Floor(GameCode / Math.Pow(10, 8)) % Math.Pow(10, 4));
            Date = (int)(GameCode % Math.Pow(10, 8));

            // Set this team
            if (HomeCode == thisTeam.TeamCode)
            {
                HomeTeam = thisTeam;
                HomeData[Program.N_DATA_PTS - 1] = HomeTeam.ConfCode;
            }
            else if (VisitorCode == thisTeam.TeamCode)
            {
                VisitorTeam = thisTeam;
                VisitorData[Program.N_DATA_PTS - 1] = VisitorTeam.ConfCode;
            }
            else
                Console.WriteLine("WARNING: Team code is neither home or visitor { Class: Game | Function: Game() }");


            // Get visitor full game data
            int row = 0;
            while ((int)allData[row][Program.TEAM_CODE] != VisitorCode) // increment to team
                row++;
            while ((long)allData[row][Program.GAME_CODE] != GameCode)   // then find this game
                row++;
            for (int i = 0; i < Program.N_DATA_PTS - Program.XTRA_DATA_PTS; i++)
                VisitorData[i] = allData[row][i];
            VisitorData[Program.TOTAL_YARDS] = allData[row][Program.RUSH_YARD] + allData[row][Program.PASS_YARD];
            VisitorData[Program.TO_AGAINST] = allData[row][Program.FUMBLE_LOST] + allData[row][Program.PASS_INT];
            VisitorData[Program.TO_FOR] = allData[row][Program.FUM_RET] + allData[row][Program.INT_RET];
            VisitorData[Program.TO_NET] = VisitorData[Program.TO_FOR] - VisitorData[Program.TO_AGAINST];

            // Get home full game data
            row = 0;
            while ((int)allData[row][Program.TEAM_CODE] != HomeCode)    // increment to team
                row++;
            while ((long)allData[row][Program.GAME_CODE] != GameCode)   // then find this game
                row++;
            for (int i = 0; i < Program.N_DATA_PTS - Program.XTRA_DATA_PTS; i++)
                HomeData[i] = allData[row][i];
            HomeData[Program.TOTAL_YARDS] = allData[row][Program.RUSH_YARD] + allData[row][Program.PASS_YARD];
            HomeData[Program.TO_AGAINST] = allData[row][Program.FUMBLE_LOST] + allData[row][Program.PASS_INT];
            HomeData[Program.TO_FOR] = allData[row][Program.FUM_RET] + allData[row][Program.INT_RET];
            HomeData[Program.TO_NET] = HomeData[Program.TO_FOR] - HomeData[Program.TO_AGAINST];

            // Determine winner
            if (HomeData[Program.POINTS] > VisitorData[Program.POINTS])
                HomeWin = true;
            else if (VisitorData[Program.POINTS] > HomeData[Program.POINTS])
                VisitorWin = true;
            else
                Tie = true;

            HomeData[Program.IS_HOME] = 1;
            VisitorData[Program.IS_HOME] = 0;

            // Passing (Home)
            if (HomeData[Program.PASS_ATT] == 0)        // no passes thrown
            {
                HomeData[Program.PASSING_EFF] = 0;
                HomeData[Program.ADJ_PASS_AVG] = 0;
            }
            else
            {
                double pYrd = HomeData[Program.PASS_YARD];
                double pTD = HomeData[Program.PASS_TD];
                double pInt = HomeData[Program.PASS_INT];
                double pCmp = HomeData[Program.PASS_COMP];
                double pAtt = HomeData[Program.PASS_ATT];
                double skYrd = HomeData[Program.SACK_YARD];
                double sacks = HomeData[Program.SACK];
                HomeData[Program.PASSING_EFF] = ((8.4 * pYrd) + (330 * pTD) - (200 * pInt) + (100 * pCmp)) / pAtt;
                HomeData[Program.ADJ_PASS_AVG] = (pYrd + 20 * pTD - 45 * pInt - skYrd) / (pAtt + sacks);
            }

            // Rushing (Home)
            if (HomeData[Program.RUSH_ATT] == 0)        // no rush attempts
            {
                HomeData[Program.ADJ_RUSH_AVG] = 0;
            }
            else
            {
                double rYrd = HomeData[Program.RUSH_YARD];
                double rTD = HomeData[Program.RUSH_TD];
                double rFirst = HomeData[Program.FIRST_DOWN_RUSH];
                double rAtt = HomeData[Program.RUSH_ATT];
                HomeData[Program.ADJ_RUSH_AVG] = (rYrd + 20 * rTD + 9 * rFirst) / rAtt;
            }

            // Passing (Visitor)
            if (VisitorData[Program.PASS_ATT] == 0)     // no passes thrown
            {
                VisitorData[Program.PASSING_EFF] = 0;
                VisitorData[Program.ADJ_PASS_AVG] = 0;
            }
            else
            {
                double pYrd = VisitorData[Program.PASS_YARD];
                double pTD = VisitorData[Program.PASS_TD];
                double pInt = VisitorData[Program.PASS_INT];
                double pCmp = VisitorData[Program.PASS_COMP];
                double pAtt = VisitorData[Program.PASS_ATT];
                double skYrd = VisitorData[Program.SACK_YARD];
                double sacks = VisitorData[Program.SACK];
                VisitorData[Program.PASSING_EFF] = ((8.4 * pYrd) + (330 * pTD) - (200 * pInt) + (100 * pCmp)) / pAtt;
                VisitorData[Program.ADJ_PASS_AVG] = (pYrd + 20 * pTD - 45 * pInt - skYrd) / (pAtt + sacks);
            }

            // Rushing (Visitor)
            if (VisitorData[Program.RUSH_ATT] == 0)        // no rush attempts
            {
                VisitorData[Program.ADJ_RUSH_AVG] = 0;
            }
            else
            {
                double rYrd = VisitorData[Program.RUSH_YARD];
                double rTD = VisitorData[Program.RUSH_TD];
                double rFirst = VisitorData[Program.FIRST_DOWN_RUSH];
                double rAtt = VisitorData[Program.RUSH_ATT];
                VisitorData[Program.ADJ_RUSH_AVG] = (rYrd + 20 * rTD + 9 * rFirst) / rAtt;
            }
        }

        //
        // Adds either visitor or home team
        public void AddTeam(Team team)
        {
            if (HomeCode == team.TeamCode)
            {
                HomeTeam = team;
                HomeData[Program.N_DATA_PTS - Program.XTRA_DATA_PTS] = HomeTeam.ConfCode;
            }
            else if (VisitorCode == team.TeamCode)
            {
                VisitorTeam = team;
                VisitorData[Program.N_DATA_PTS - Program.XTRA_DATA_PTS] = VisitorTeam.ConfCode;
            }
            else
                Console.WriteLine("WARNING: Team code is neither home or visitor { Class: Game | Function: AddTeam() }");
        }

        //
        // Gets the ratio of offensive yards normalized by the defense's average yardage given up (prior to this game)
        public void GetYardageRatio()
        {
            // Get offensive averages
            double homeYards = HomeTeam.GetAverage(Program.TOTAL_YARDS, true, this);
            double visitorYards = VisitorTeam.GetAverage(Program.TOTAL_YARDS, true, this);

            // Get defensive averages
            double homeDefense = HomeTeam.GetAverage(Program.TOTAL_YARDS, false, this);
            double visitorDefense = VisitorTeam.GetAverage(Program.TOTAL_YARDS, false, this);

            // Set ratios
            HomeData[Program.YARD_RATIO] = homeYards / visitorDefense;
            VisitorData[Program.YARD_RATIO] = visitorYards / homeDefense;
        }

        //
        // Gets the pythagorean expectation of the teams' OOC schedule
        public void GetPythagoreanOOC(int[] dates)
        {
            // Set expectation
            HomeData[Program.OOC_PYTHAG] = HomeTeam.Conf.GetPythagoreanOOC(dates);
            VisitorData[Program.OOC_PYTHAG] = VisitorTeam.Conf.GetPythagoreanOOC(dates);

            // Set ratios
            if (VisitorData[Program.OOC_PYTHAG] > 0)    // set home
                HomeData[Program.OOC_PYTHAG_RATIO] = HomeData[Program.OOC_PYTHAG] / VisitorData[Program.OOC_PYTHAG];
            else
                HomeData[Program.OOC_PYTHAG_RATIO] = Program.PY_RATIO_CAP;
            
            if (HomeData[Program.OOC_PYTHAG] > 0)       // set visitor
                VisitorData[Program.OOC_PYTHAG_RATIO] = VisitorData[Program.OOC_PYTHAG] / HomeData[Program.OOC_PYTHAG];
            else
                VisitorData[Program.OOC_PYTHAG_RATIO] = Program.PY_RATIO_CAP;
        }

        //
        // Gets each teams pythagorean percentage within a date range
        public void GetPythagoreanExpectation(int[] dates)
        {
            // Home
            double homeRS = HomeTeam.GetTotal(Program.POINTS, true, dates);     // home offense
            double homeRA = HomeTeam.GetTotal(Program.POINTS, false, dates);    // home defense
            HomeData[Program.PYTHAG_EXPECT] = Math.Pow(homeRS, 2.37) / (Math.Pow(homeRS, 2.37) + Math.Pow(homeRA, 2.37));

            // Visitor
            double visitorRS = VisitorTeam.GetTotal(Program.POINTS, true, dates);   // visitor offense
            double visitorRA = VisitorTeam.GetTotal(Program.POINTS, false, dates);  // visitor defense
            VisitorData[Program.PYTHAG_EXPECT] = Math.Pow(visitorRS, 2.37) / (Math.Pow(visitorRS, 2.37) + Math.Pow(visitorRA, 2.37));
        }

        //
        // Gets the home and visitor pythagorean percentage
        public void GetHomeVisitorPyEx(int[] dates)
        {
            double homeRS = 0, homeRA = 0;
            double visitorRS = 0, visitorRA = 0;

            // Find sums of points against and scored (Home)
            foreach (Game G in HomeTeam.Games)
            {
                // Too early or too late
                if (G.Date < dates[0] || G.Date >= dates[1])
                    continue;

                if (G.HomeTeam.TeamCode == HomeTeam.TeamCode && G.GameCode != GameCode)
                {
                    homeRS += G.HomeData[Program.POINTS];
                    homeRA += G.VisitorData[Program.POINTS];
                }
            }
            // Do calculation
            double divisor = Math.Pow(homeRS, 2.37) + Math.Pow(homeRA, 2.37);
            if (divisor > 0)
                HomeData[Program.HV_PY_EXPECT] = Math.Pow(homeRS, 2.37) / divisor;
            else
                HomeData[Program.HV_PY_EXPECT] = 0.5;

            // Find sums of points against and scored (visitor)
            foreach (Game G in VisitorTeam.Games)
            {
                // Too early or too late
                if (G.Date < dates[0] || G.Date >= dates[1])
                    continue;

                if (G.VisitorTeam.TeamCode == VisitorTeam.TeamCode && G.GameCode != GameCode)
                {
                    visitorRS += G.HomeData[Program.POINTS];
                    visitorRA += G.VisitorData[Program.POINTS];
                }
            }
            // Do calculation
            divisor = Math.Pow(visitorRS, 2.37) + Math.Pow(visitorRA, 2.37);
            if (divisor > 0)
                VisitorData[Program.HV_PY_EXPECT] = Math.Pow(visitorRS, 2.37) / divisor;
            else
                VisitorData[Program.HV_PY_EXPECT] = 0.5;

            HomeData[Program.HOME_TIMES_HVPE] = HomeData[Program.HV_PY_EXPECT];
            VisitorData[Program.HOME_TIMES_HVPE] = 0;
        }
    }
}