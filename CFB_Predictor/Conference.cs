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
        // Gets the Pythagorean expectation for this conference in OOC games
        public double GetOOCPythagorean()
        {
            double sumPyE = 0, nGames = 0;
            foreach (Team T in Teams)
            {
                foreach (Game G in T.Games)
                {
                    // Is an OOC game
                    if (G.HomeTeam.ConfCode != G.VisitorTeam.ConfCode)
                    {
                        nGames++;
                        double RS, RA;
                        if (G.HomeTeam.ConfCode == Code)
                        {
                            RS = G.HomeData[Program.POINTS];
                            RA = G.VisitorData[Program.POINTS];
                        }
                        else
                        {
                            RS = G.VisitorData[Program.POINTS];
                            RA = G.HomeData[Program.POINTS];
                        }

                        sumPyE += Math.Pow(RS, 2.37) / (Math.Pow(RS, 2.37) + Math.Pow(RA, 2.37));
                    }
                }
            }

            return sumPyE / nGames;
        }
    }
}