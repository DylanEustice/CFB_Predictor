using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Team
    {
        public int TeamCode;
        public string TeamName;
        public int ConfCode;
        public Conference Conf;
        public Game[] Games;
        public int[] Record = { 0, 0 };     // [W,L]
        public List<Season> PastSeasons = new List<Season>();

        //
        // Constructor
        public Team(int teamCode)
        {
            TeamCode = teamCode;
        }
        public Team(int teamCode, string teamName, int confCode)
        {
            TeamCode = teamCode;
            TeamName = teamName;
            ConfCode = confCode;
        }

        //
        // Adds a list of seasons with only this teams game data
        public void GetPastSeasons(Season[] pastSeasons)
        {
            foreach (Season S in pastSeasons)
            {
                int pastYear = S.Year;  // set the year

                // Find this team
                int i = 0;
                while (S.Teams[i].TeamCode != TeamCode)
                    i++;
                Team[] pastTeam = { S.Teams[i] };
                Game[] pastGames = S.Teams[i].Games;
                Season pastSeason = new Season(pastYear, pastTeam, pastGames);
                PastSeasons.Add(pastSeason);
            }
        }

        //
        // Fills the Games list
        public void GetGames(double[][] allData, Team[] allTeams)
        {
            List<Game> gameList = new List<Game>();
            int row = 0;
            while (row < allData.Length)
            {
                // Found a new game
                if (TeamCode == allData[row][Program.TEAM_CODE])
                {
                    Game newGame = new Game(allData[row][Program.GAME_CODE], allData, this);
                    Team opponent = new Team(0);

                    // Find the opponent in this game
                    int row2 = 0;
                    while (row2 < allData.Length)
                    {
                        // Found the other team's game
                        if (allData[row2][Program.GAME_CODE] == newGame.GameCode && allData[row2][Program.TEAM_CODE] != TeamCode)
                        {
                            int oppCode = (int)allData[row2][Program.TEAM_CODE];
                            foreach (Team T in allTeams)
                            {
                                // Found the other team
                                if (T.TeamCode == oppCode)
                                {
                                    opponent = T;
                                    break;  // done looking for the team
                                }
                            }
                            break;  // done looking for the game
                        }
                        row2++;
                    }
                    newGame.AddTeam(opponent);
                    if (newGame.VisitorTeam.TeamCode == newGame.HomeTeam.TeamCode)
                        Console.WriteLine("WARNING: newGame contains the same two teams in GetGames()");

                    gameList.Add(newGame);
                }
                row++;
            }
            Games = gameList.ToArray();
            GetRecord();
        }

        //
        // Gets this teams wins and losses
        public void GetRecord()
        {
            foreach (Game g in Games)
                if ((g.HomeCode == TeamCode && g.HomeWin) || (g.VisitorCode == TeamCode && g.VisitorWin))
                    Record[0]++;    // win
                else
                    Record[1]++;    // loss
        }

        //
        // Gets the average of either team, using only data from weeks prior
        public double GetAverage(int stat, bool thisTeam, Game game)
        {
            int date = game.Date;

            // Intelligently find averages
            if (stat == Program.IS_HOME)
            {
                // Return proper data
                if (TeamCode == game.HomeCode)
                    return 1;
                else
                    return 0;
            }
            else if (stat == Program.OOC_PYTHAG)
            {
                int beginLastSeason = PastSeasons[0].Games[0].Date; // First game of previous season
                int[] dates = { beginLastSeason, date };            // set range of dates
                game.GetPythagoreanOOC(dates);                      // set pythagorean data

                // Return proper data
                if (TeamCode == game.HomeCode)
                    return game.HomeData[Program.OOC_PYTHAG];
                else
                    return game.VisitorData[Program.OOC_PYTHAG];
                
            }
            else if (stat == Program.OOC_PYTHAG_RATIO)
            {
                // Get home team Py OOC
                int beginLastSeason = game.HomeTeam.PastSeasons[0].Games[0].Date;   // First game of previous season
                int[] dates = { beginLastSeason, date };                            // set range of dates
                double homePyOOC = game.HomeTeam.Conf.GetPythagoreanOOC(dates);

                // Get visitor team Py OOC
                beginLastSeason = game.VisitorTeam.PastSeasons[0].Games[0].Date;    // First game of previous season
                dates[0] = beginLastSeason;                                         // set range of dates
                double visitorPyOOC = game.HomeTeam.Conf.GetPythagoreanOOC(dates);

                // Return proper ratio
                if (TeamCode == game.HomeCode)
                    return game.HomeData[Program.OOC_PYTHAG_RATIO];
                else
                    return game.VisitorData[Program.OOC_PYTHAG_RATIO];
            }
            else if (stat == Program.PYTHAG_EXPECT)
            {
                // If there weren't enough games this season, use last season as well
                int[] dates = new int[2];
                dates[1] = game.Date;
                if (Games[1].Date < game.Date)
                    dates[0] = Games[0].Date;
                else
                    dates[0] = PastSeasons[0].Games[0].Date;
                game.GetPythagoreanExpectation(dates);

                // Return proper data
                if (TeamCode == game.HomeCode)
                    return game.HomeData[Program.PYTHAG_EXPECT];
                else
                    return game.VisitorData[Program.PYTHAG_EXPECT];
            }
            else if (stat == Program.HV_PY_EXPECT || stat == Program.HOME_TIMES_HVPE)
            {
                int returnType = stat;

                // If there weren't enough games this season, use last season as well
                int[] dates = new int[2];
                dates[1] = game.Date;
                if (Games[1].Date < game.Date)
                    dates[0] = Games[0].Date;
                else
                    dates[0] = PastSeasons[0].Games[0].Date;
                game.GetHomeVisitorPyEx(dates);

                // Return proper data
                if (TeamCode == game.HomeCode)
                    return game.HomeData[returnType];
                else
                    return game.VisitorData[returnType];
            }
            else if (stat == Program.YARD_RATIO)
            {
                game.GetYardageRatio();
                if (TeamCode == game.HomeCode)
                    return game.HomeData[Program.YARD_RATIO];
                else
                    return game.VisitorData[Program.YARD_RATIO];

            }

            double tot = 0;

            // Try to use this season
            int nGames = 0;
            foreach (Game G in Games)
            {
                // Only add games prior to this one
                if (G.Date >= date || G.HomeTeam.Conf.Division == "FCS" || G.VisitorTeam.Conf.Division == "FCS")
                    continue;

                nGames++;
                if (thisTeam)
                    tot += AddStat(G, stat);
                else
                    tot += AddOppStat(G, stat);
            }
            if (nGames > 1)
            {
                return tot / nGames;
            }
            else    // If there weren't enough games this season, use last season as well
            {
                foreach (Game G in PastSeasons[0].Games)
                {
                    // Only add games prior to this one
                    if (G.Date >= date || G.HomeTeam.Conf.Division == "FCS" || G.VisitorTeam.Conf.Division == "FCS")
                        continue;

                    nGames++;
                    if (thisTeam)
                        tot += AddStat(G, stat);
                    else
                        tot += AddOppStat(G, stat);
                }
                if (nGames > 0)
                    return tot / nGames;
                else
                {
                    Console.WriteLine("WARNING: No stats for for this game { Class: Team | Function: GetAverage() }");
                    Console.WriteLine("         Team: {0}", TeamName);
                    return Double.NaN;
                }
            }
        }

        //
        // Gets the total from all games but that included
        public double GetTotal(int stat, bool thisTeam, int[] dates)
        {
            // Add games to list within date range
            double tot = 0;
            List<Game> allGames = new List<Game>();
            foreach (Season S in PastSeasons)
            {
                foreach (Game G in S.Teams[0].Games)
                {
                    // Too early or too late
                    if (G.Date < dates[0] || G.Date >= dates[1])
                        continue;
                    else
                        allGames.Add(G);
                }
            }
            foreach (Game G in Games)
            {
                // Too early or too late
                if (G.Date < dates[0] || G.Date >= dates[1])
                    continue;
                else
                    allGames.Add(G);
            }

            // Find sums
            foreach (Game G in allGames)
            {
                if (thisTeam)
                    tot += AddStat(G, stat);
                else
                    tot += AddOppStat(G, stat);
            }
            return tot;
        }

        //
        // Returns game stat for this team, determining whether to use home or visitor data
        public double AddStat(Game game, int stat)
        {
            if (game.HomeCode == TeamCode)
                return game.HomeData[stat];
            else if (game.VisitorCode == TeamCode)
                return game.VisitorData[stat];
            else
            {
                Console.WriteLine("WARNING: Team code is neither home or visitor { Class: Team | Function: AddStat() }");
                return 0;
            }
        }

        //
        // Returns game stat for opposing team, determining whether to use home or visitor data
        public double AddOppStat(Game game, int stat)
        {
            if (game.HomeCode == TeamCode)
                return game.VisitorData[stat];
            else if (game.VisitorCode == TeamCode)
                return game.HomeData[stat];
            else
            {
                Console.WriteLine("WARNING: Team code is neither home or visitor { Class: Team | Function: AddStat() }");
                return 0;
            }
        }
    }
}