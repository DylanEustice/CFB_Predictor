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
        // Gets the average of a particular stat
        public double GetAverage(int stat)
        {
            double tot = 0;
            foreach (Game g in Games)
                tot += AddStat(g, stat);
            return tot / Games.Length;
        }
        // Option to take out game performance from data
        public double GetAverage(int stat, double gameStat)
        {
            // Check if this is a game stat
            foreach (int useGame in Program.USE_GAME_STAT)
                if (useGame == gameStat)
                    return gameStat;

            // List of stats that are not averaged
            if (stat == Program.IS_HOME)
                return gameStat;

            double tot = 0;
            if (Games.Length > 1)
            {
                foreach (Game g in Games)
                    tot += AddStat(g, stat);
                return (tot - gameStat) / (Games.Length - 1);
            }
            else
                return AddStat(Games[0], stat);
        }
        // Option to get averages from the opposing teams
        //  - This reverses type of stat (eg. offensive pass yards by this team -> defensive pass yards)
        public double GetAverage(int stat, bool thisTeam)
        {
            double tot = 0;
            foreach (Game g in Games)
            {
                if (thisTeam)
                    tot += AddStat(g, stat);
                else
                    tot += AddOppStat(g, stat);
            }
            return tot / Games.Length;
        }
        // Option to take out this game performance from data
        public double GetAverage(int stat, bool thisTeam, double gameStat)
        {
            // Check if this is a game stat
            foreach (int useGame in Program.USE_GAME_STAT)
                if (useGame == gameStat)
                    return gameStat;

            // List of stats that are not averaged
            if (stat == Program.IS_HOME)
                return gameStat;

            double tot = 0;
            if (Games.Length > 1)
            {
                foreach (Game g in Games)
                {
                    if (thisTeam)
                        tot += AddStat(g, stat);
                    else
                        tot += AddOppStat(g, stat);
                }
                return (tot - gameStat) / (Games.Length - 1);
            }
            else
            {
                //Console.WriteLine("WARNING: This is " + TeamName + "'s only game, finding average returns this game.");
                if (thisTeam)
                    return AddStat(Games[0], stat);
                else
                    return AddOppStat(Games[0], stat);
            }
        }
        // Gets the average of either team, using only data from weeks prior
        public double GetAverage(int stat, bool thisTeam, double gameStat, int date)
        {
            // Check if this is a game stat
            foreach (int useGame in Program.USE_GAME_STAT)
                if (useGame == gameStat)
                    return gameStat;

            // List of stats that are not averaged
            if (stat == Program.IS_HOME)
                return gameStat;

            double tot = 0;
            if (Games.Length > 1)
            {
                int nGames = 0;
                foreach (Game g in Games)
                {
                    // Only add games prior to this one
                    if (g.Date >= date || g.HomeTeam.Conf.Division == "FCS" || g.VisitorTeam.Conf.Division == "FCS")
                        continue;

                    nGames++;
                    if (thisTeam)
                        tot += AddStat(g, stat);
                    else
                        tot += AddOppStat(g, stat);
                }
                return (tot - gameStat) / nGames;
            }
            else
            {
                Console.WriteLine("WARNING: This is " + TeamName + "'s only game, finding average returns this game.");
                if (thisTeam)
                    return AddStat(Games[0], stat);
                else
                    return AddOppStat(Games[0], stat);
            }
        }

        //
        // Gets the total from all games but that included
        public double GetTotal(int stat, double gameStat)
        {
            double tot = 0;
            if (Games.Length > 1)
            {
                foreach (Game g in Games)
                    tot += AddStat(g, stat);
                return tot - gameStat;
            }
            else
                return AddStat(Games[0], stat);
        }
        // Option to get data from opponent
        public double GetTotal(int stat, bool thisTeam, double gameStat)
        {
            double tot = 0;
            if (Games.Length > 1)
            {
                foreach (Game g in Games)
                {
                    if (thisTeam)
                        tot += AddStat(g, stat);
                    else
                        tot += AddOppStat(g, stat);
                }
                return tot - gameStat;
            }
            else
            {
                //Console.WriteLine("WARNING: This is " + TeamName + "'s only game, finding average returns this game.");
                if (thisTeam)
                    return AddStat(Games[0], stat);
                else
                    return AddOppStat(Games[0], stat);
            }
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