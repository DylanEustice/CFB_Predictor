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
    public class Season
    {
        public int Year;
        public Conference[] Conferences;
        public Team[] Teams;
        public Game[] Games;
        public double[][] TeamGameData;

        //
        // Constructor
        public Season(int year)
        {
            Year = year;
            Console.WriteLine("Reading data and building teams from {0}\n", Year);

            // Read in yearly data file
            string yearString = Year.ToString();
            string dataType = "team-game-statistics";
            string ext = ".csv";
            string file = "../../Statistics" + "/" + yearString + "/" + dataType + ext;
            List<double[]> TeamGameDataList = new List<double[]>();
            using (TextFieldParser cfbParser = new TextFieldParser(file))
            {
                cfbParser.TextFieldType = FieldType.Delimited;
                cfbParser.SetDelimiters(",");
                string[] headers = cfbParser.ReadFields();
                while (!cfbParser.EndOfData)
                {
                    // Processing row
                    string[] fields = cfbParser.ReadFields();
                    double[] thisData = new double[fields.Length];
                    for (int i = 0; i < fields.Length; i++)
                        thisData[i] = Convert.ToDouble(fields[i]);
                    TeamGameDataList.Add(thisData);
                }
            }
            TeamGameData = TeamGameDataList.ToArray();

            // Read in team data from file
            dataType = "team";
            file = "../../Statistics" + "/" + yearString + "/" + dataType + ext;
            List<Team> TeamList = new List<Team>();
            using (TextFieldParser cfbParser = new TextFieldParser(file))
            {
                cfbParser.TextFieldType = FieldType.Delimited;
                cfbParser.SetDelimiters(",");
                string[] headers = cfbParser.ReadFields();
                while (!cfbParser.EndOfData)
                {
                    // Processing row
                    string[] fields = cfbParser.ReadFields();
                    int thisTeamCode = (int)Convert.ToDouble(fields[0]);
                    string thisTeamName = fields[1];
                    int thisTeamConfCode = (int)Convert.ToDouble(fields[2]);
                    Team thisTeam = new Team(thisTeamCode, thisTeamName, thisTeamConfCode);
                    TeamList.Add(thisTeam);
                }
            }
            Teams = TeamList.ToArray();      // finish building teams
            for (int i = 0; i < Teams.Length; i++)
                Teams[i].GetGames(TeamGameData, Teams);

            // Read in conference data from file
            dataType = "conference";
            file = "../../Statistics" + "/" + yearString + "/" + dataType + ext;
            List<Conference> ConfList = new List<Conference>();
            using (TextFieldParser cfbParser = new TextFieldParser(file))
            {
                cfbParser.TextFieldType = FieldType.Delimited;
                cfbParser.SetDelimiters(",");
                string[] headers = cfbParser.ReadFields();
                while (!cfbParser.EndOfData)
                {
                    // Processing row
                    string[] fields = cfbParser.ReadFields();
                    int thisConfCode = (int)Convert.ToDouble(fields[0]);
                    string thisConfName = fields[1];
                    string thisConfDiv = fields[2];
                    Conference thisConf = new Conference(thisConfCode, thisConfName, thisConfDiv, Teams);
                    ConfList.Add(thisConf);
                }
            }
            Conferences = ConfList.ToArray();

            // Add conferences to teams
            foreach (Conference C in Conferences)
            {
                for (int i = 0; i < Teams.Length; i++)
                {
                    if (Teams[i].ConfCode == C.Code)
                        Teams[i].Conf = C;
                }
            }

            // Stuff that needs to be done after everything else
            foreach (Team T in Teams)
            {
                foreach (Game G in T.Games)
                {
                    G.GetYardageRatio();
                    G.GetPythagoreanOOC();
                    G.GetPythagoreanExpectation();
                    G.GetHomeVisitorPyEx();
                    G.HomeData[Program.BIAS] = 1;
                    G.VisitorData[Program.BIAS] = 1;
                }
            }
            Games = BuildGames(Teams);
        }

        //
        // Builds game array
        public static Game[] BuildGames(Team[] Teams)
        {
            List<Game> GameList = new List<Game>();
            for (int i = 0; i < Teams.Length; i++) // loop through all teams
            {
                for (int j = 0; j < Teams[i].Games.Length; j++) // loop through this team's games
                {
                    bool newGame = true;
                    for (int k = 0; k < GameList.Count; k++) // check if this game has already been added
                    {
                        if (GameList[k].GameCode == Teams[i].Games[j].GameCode)
                        {
                            newGame = false;
                            GameList[k].AddTeam(Teams[i]);  // check this team in
                            break;
                        }
                    }
                    if (newGame)    // add game to list otherwise
                    {
                        GameList.Add(Teams[i].Games[j]);
                    }
                }
            }
            // Make sure all games are good to go
            for (int i = 0; i < GameList.Count; i++)
            {
                if (GameList[i].HomeTeam == null || GameList[i].VisitorTeam == null)
                {
                    GameList.Remove(GameList[i]);
                    Console.WriteLine("WARNING: Removed game {0} due to a NULL team", i);
                    i--;
                    continue;
                }
            }
            return GameList.ToArray();
        }
    }
}