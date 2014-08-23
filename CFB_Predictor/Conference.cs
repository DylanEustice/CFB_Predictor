using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Conference
    {
        public int Code;
        public string Name;
        public string Division;
        public Team[] Teams;
        public List<Season> PastSeasons = new List<Season>();

        //
        // Constructor
        public Conference(int code, string name, string division, Team[] allTeams)
        {
            Code = code;
            Name = name;
            Division = division;
            ExtractConfTeams(allTeams);
        }

        //
        // Takes an input of an array of teams and picks out those in this conference. (Re)assigns Teams array.
        public void ExtractConfTeams(Team[] allTeams)
        {
            List<Team> teamList = new List<Team>();
            foreach (Team T in allTeams)
            {
                if (T.ConfCode == Code)
                    teamList.Add(T);
            }
            Teams = teamList.ToArray();
        }

        //
        // Adds a list of seasons with only this conference game data
        public void GetPastSeasons(Season[] pastSeasons)
        {
            foreach (Season S in pastSeasons)
            {
                int pastYear = S.Year;  // set the year

                // Find all teams in this conference
                List<Team> pastTeamsList = new List<Team>();
                foreach (Team T in S.Teams)
                    if (T.ConfCode == Code)
                        pastTeamsList.Add(T);

                // Add all games to the list
                List<Game> pastGamesList = new List<Game>();
                foreach (Team T in pastTeamsList)
                    foreach (Game G in T.Games)
                        pastGamesList.Add(G);

                Team[] pastTeams = pastTeamsList.ToArray();
                Game[] pastGames = pastGamesList.ToArray();
                Season pastSeason = new Season(pastYear, pastTeams, pastGames);
                PastSeasons.Add(pastSeason);
            }
        }

        //
        // Gets the OOC PE for this conference using only games within a range of games
        public double GetPythagoreanOOC(int[] dates)
        {
            double sumRS = 0, sumRA = 0;

            // Sum games from past seasons
            foreach (Season S in PastSeasons)
            {
                foreach (Team T in S.Teams)
                {
                    foreach (Game G in T.Games)
                    {
                        // Only get FBS games
                        if (G.HomeTeam.Conf.Division == "FCS" || G.VisitorTeam.Conf.Division == "FCS")
                            continue;

                        // Too early or too late
                        if (G.Date < dates[0] || G.Date >= dates[1])
                            continue;

                        // Is an OOC game
                        if (G.HomeTeam.ConfCode != G.VisitorTeam.ConfCode)
                        {
                            if (G.HomeTeam.ConfCode == Code)
                            {
                                sumRS += G.HomeData[Program.POINTS];
                                sumRA += G.VisitorData[Program.POINTS];
                            }
                            else if (G.VisitorTeam.ConfCode == Code)
                            {
                                sumRS += G.VisitorData[Program.POINTS];
                                sumRA += G.HomeData[Program.POINTS];
                            }
                        }
                    }
                }
            }
            foreach (Team T in Teams)
            {
                foreach (Game G in T.Games)
                {
                    // Only get FBS games
                    if (G.HomeTeam.Conf.Division == "FCS" || G.VisitorTeam.Conf.Division == "FCS")
                        continue;

                    // Too early or too late
                    if (G.Date < dates[0] || G.Date >= dates[1])
                        continue;

                    // Is an OOC game
                    if (G.HomeTeam.ConfCode != G.VisitorTeam.ConfCode)
                    {
                        if (G.HomeTeam.ConfCode == Code)
                        {
                            sumRS += G.HomeData[Program.POINTS];
                            sumRA += G.VisitorData[Program.POINTS];
                        }
                        else if (G.VisitorTeam.ConfCode == Code)
                        {
                            sumRS += G.VisitorData[Program.POINTS];
                            sumRA += G.HomeData[Program.POINTS];
                        }
                    }
                }
            }

            return Math.Pow(sumRS, 2.37) / (Math.Pow(sumRS, 2.37) + Math.Pow(sumRA, 2.37));
        }
    }
}