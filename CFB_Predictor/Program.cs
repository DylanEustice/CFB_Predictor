////////////////////////////////////////////////////////////////////////////////
//
// COLLEGE FOOTBALL PREDICTION ALGORITHM
//  Using a neural network trained with particle swarm to predict
//  outcomes and scores of college football
//
// Author:          Dylan Eustice
// Date Created:    5/23/2014
// Last Edited:     8/12/2014
//
////////////////////////////////////////////////////////////////////////////////


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
        // Particle Swarm and Neural Network parameters
        public const double MAX_WEIGHT      = 1.0;          // max initial weight of neural network synapse
        public const double MIN_WEIGHT      = -1.0;         // min initial weight of neural network synapse
        public const double RESET_WEIGHT    = 0.05;         // factor by which max weights are increased every reset
        public const double MIN_MOVEMENT    = 0.01;         // min average movement by the swarm
        public const double MAX_MOVEMENT    = 0.1;          // max movement for the neural network. Set to 0 to ignore

        public const int N_PARTICLES        = 200;          // number of swarm particles
        public const int ITERATIONS         = 2000;         // number of swarm steps
                                                           
        public const double MOMENTUM        = 0.9;          // MOMENTUM of a particle
        public const double MAX_GLOBAL      = 0.2;          // MOMENTUM towards global best
        public const double MAX_PERSONAL    = 0.2;          // MOMENTUM towards personal best
                                                           
        public const double TEST_RATIO      = 0.5;          // percentage of games used for testing
        public const bool NORMALIZE         = true;         // chooses whether to normalize input and output data
        public const int TRAIN_TYPE         = RAND;         // chooses whether to train using home, visitor, or random

        public const int RUN                = SIM_DATES;    // TRAIN | DUMB_ACC | SIM_DATES
        public const string SAVE_TO         = "Network_4.csv";

        public const int BEGIN_NETWORKS     = 1;            // starting number of 1st network to read in
        public const int END_NETWORKS       = 4;            // starting number of last network to read in

        public const int PREDICTION_TYPE    = PTS_AVG;      // DIF | SIM_AVG | PTS_AVG
        public const int N_SIM_GAMES        = 50;           // games to find similarities with when finding a network error

        public readonly static int[] ACCRY_DATES = { WEEKS_2013[5][0], WEEKS_2013[16][0] };
        public readonly static int[] TRAIN_DATES = { WEEKS_2013[0][0], WEEKS_2013[16][0] };
        public readonly static int[] ALL_DATES = { 0, int.MaxValue };

        public const double CORRECT_FACTOR  = 1.0;          // factor for which a network's error is changed if the game is correct
        public const double DIF_CAP         = 21;           // caps game difference in GetGameDifference()
        public const double PY_RATIO_CAP    = 5;            // cap on OOC pythag calcs when a divisor is 0

        public const double USE_BIAS        = 1;

        static void Main(string[] args)
        {
            // ================================================================================
            //   BUILD SEASON DATA   ==========================================================
            // ================================================================================
            Season season2012 = new Season(2012);
            Season season2013 = new Season(2013);
            Season[] pastSeasons = { season2012 };
            season2013.AddPastSeasons(pastSeasons);
            
            // Make a list of training games
            Game[] trainGames1 = GetGoodGames(season2012, ALL_DATES);
            Game[] trainGames2 = GetGoodGames(season2013, TRAIN_DATES);
            List<Game> trainList = new List<Game>();
            //foreach (Game g in trainGames1)
                //trainList.Add(g);
            foreach (Game g in trainGames2)
                trainList.Add(g);

            // Finalize training and accuracy games
            Game[] trainGames = trainList.ToArray();
            Game[] accGames = GetGoodGames(season2013, ACCRY_DATES);

            // Input statistics
            // ft = this team's for stats
            // tt = opp team's for stats
            // tf = opp team's against stats
            // ff = this team's against stats
            int[] inputStat = { 
                                TOTAL_YARDS,        TOTAL_YARDS,
                                ADJ_PASS_AVG,       ADJ_PASS_AVG,
                                ADJ_RUSH_AVG,       ADJ_RUSH_AVG,
                                POINTS,             POINTS,
                                //YARD_RATIO,         YARD_RATIO,
                                IS_HOME, 
                                HV_PY_EXPECT,       HV_PY_EXPECT,
                                //HOME_TIMES_HVPE,
                                OOC_PYTHAG,         OOC_PYTHAG,
                                //OOC_PYTHAG_RATIO,
                                PYTHAG_EXPECT,      PYTHAG_EXPECT,
                                PENALTY_YARD,       PENALTY_YARD,
                                //TO_NET,             TO_NET,
                                //TO_FOR,             TO_FOR,
                                //TO_AGAINST,         TO_AGAINST,
                                //TIME_OF_POS,
                              };
            bool[] inUseOpponent = {
                                     false, true,     // TOTAL_YARDS
                                     false, true,     // ADJ_PASS_AVG
                                     false, true,     // ADJ_RUSH_AVG
                                     false, true,     // POINTS
                                     //false, true,     // YARD_RATIO
                                     false,           // IS_HOME
                                     false, true,     // HV_PY_EXPECT
                                     //false,           // HOME_TIMES_HVPE
                                     false, true,     // OOC_PYTHAG
                                     //false,           // OOC_PYTHAG_RATIO
                                     false, true,     // PYTHAG_EXPECT
                                     false, true,     // PENALTY
                                     //false, true,     // TO_NET
                                     //false, true,     // TO_FOR
                                     //false, true,     // TO_AGAINST
                                     //false,           // TIME_OF_POS
                                   };
            bool[] inUseOffense = {
                                    true, false,      // TOTAL_YARDS
                                    true, false,      // ADJ_PASS_AVG
                                    true, false,      // ADJ_RUSH_AVG
                                    true, false,      // POINTS
                                    //true, false,      // YARD_RATIO
                                    true,             // IS_HOME
                                    true, true,       // HV_PY_EXPECT
                                    //true,             // HOME_TIMES_HVPE
                                    true, true,       // OOC_PYTHAG
                                    //true,             // OOC_PYTHAG_RATIO
                                    true, true,       // PYTHAG_EXPECT
                                    true, true,       // PENALTY
                                    //true, true,       // TO_NET
                                    //true, true,       // TO_FOR
                                    //true, true,       // TO_AGAINST
                                    //true,             // TIME_OF_POS
                                  };

            int[] LayerSizes = { inputStat.Length, 2, 4, 2, 1 };   // 1st layer is # of inputs, last is # of outputs

            int[] outputStat = { POINTS };

            // Accuracy variables
            double correct = 0;
            int nGames = 0;
            string[] predictions = new string[accGames.Length];

            // ================================================================================
            //   TRAIN NEW NETWORK   ==========================================================
            // ================================================================================
            if (RUN == TRAIN)
            {
                // Get training maximums
                double[] maximumData = FindMaximums(trainGames);

                // Train network
                int nTrainingGames = (int)Math.Ceiling((1 - TEST_RATIO) * trainGames.Length);
                int nTestGames = (int)Math.Floor(TEST_RATIO * trainGames.Length);
                double[][] trainingInputData = new double[nTrainingGames][];
                double[][] trainingOutputData = new double[nTrainingGames][];
                double[][] testInputData = new double[nTestGames][];
                double[][] testOutputData = new double[nTestGames][];
                Neural_Network oracle = TrainNetwork(maximumData, trainGames, LayerSizes, inUseOpponent, inUseOffense, inputStat,
                    outputStat, ref trainingInputData, ref trainingOutputData, ref testInputData, ref testOutputData, SAVE_TO);

                // Get accuracy maximums
                maximumData = FindMaximums(accGames);

                // Get accuracy
                nGames = accGames.Length;
                correct = AnalyzeAccuracy(accGames, LayerSizes, inUseOpponent, inUseOffense, inputStat, outputStat, maximumData,
                    oracle, ref nGames, ref predictions);
            }
            // ================================================================================
            //   RUN DUMB TEST  ===============================================================
            // ================================================================================
            else if (RUN == DUMB_ACC)
            {
                // Read in networks
                List<Neural_Network> networks = new List<Neural_Network>();
                for (int i = BEGIN_NETWORKS; i <= END_NETWORKS; i++)
                {
                    string networkFile = "../../Networks/Network_" + i.ToString() + ".csv";
                    Neural_Network newNetwork = RememberNetwork(networkFile);
                    networks.Add(newNetwork);
                }

                // Get accuracy maximums
                double[] maximumData = FindMaximums(accGames);

                // Get accuracy
                nGames = accGames.Length;
                correct = AnalyzeAccuracy(accGames, maximumData, networks.ToArray(), ref nGames, ref predictions);
            }

            // ================================================================================
            //   SIMULATE RANGE OF DATES   ====================================================
            // ================================================================================
            else if (RUN == SIM_DATES)
            {
                // Read in networks
                List<Neural_Network> networkList = new List<Neural_Network>();
                for (int i = BEGIN_NETWORKS; i <= END_NETWORKS; i++)
                {
                    string path = "../../Networks/";
                    string networkFile = path + "Network_" + i.ToString() + ".csv";
                    Neural_Network newNetwork = RememberNetwork(networkFile);
                    networkList.Add(newNetwork);
                }
                Neural_Network[] networks = networkList.ToArray();

                // Get accuracy
                int[] dates = ACCRY_DATES;
                correct = SimulateWeek(season2013, dates, networks, ref predictions, ref nGames, trainGames);
            }

            double pctCorrect = 100 * correct / nGames;
            Console.WriteLine("Percent games predicted: {0}", pctCorrect);

            // Write predictions
            string predictionFileName = "../../Output/Point_Predictions.csv";
            StreamWriter predictionFileStream = new StreamWriter(predictionFileName);
            foreach (string s in predictions)
                predictionFileStream.WriteLine(s);
            predictionFileStream.Close();

            Console.WriteLine("\nPress ENTER to continue...");
            Console.ReadLine();
        }
    }
}