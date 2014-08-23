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
        //
        // Reads in a network saved to file
        public static Neural_Network RememberNetwork(string file)
        {
            List<string[]> allData = ReadCSV(file);     // read in data

            // Set Layer Sizes
            int[] LayerSizes = new int[allData[0].Length];
            for (int i = 0; i < allData[0].Length; i++)
            {
                string[] thisLayerSize = allData[0];
                LayerSizes[i] = (int)Convert.ToDouble(thisLayerSize[i]);
            }
            // Set Opponent Data Info
            bool[] UseOpponent = new bool[allData[1].Length];
            for (int i = 0; i < allData[1].Length; i++)
            {
                string[] thisUseOpponent = allData[1];
                UseOpponent[i] = (thisUseOpponent[i] == "True") ? true : false;
            }
            // Set Offense Data Info
            bool[] UseOffense = new bool[allData[2].Length];
            for (int i = 0; i < allData[2].Length; i++)
            {
                string[] thisUseOffense = allData[2];
                UseOffense[i] = (thisUseOffense[i] == "True") ? true : false;
            }
            // Set Input Info
            int[] InputStats = new int[allData[3].Length];
            for (int i = 0; i < allData[3].Length; i++)
            {
                string[] thisInputStats = allData[3];
                InputStats[i] = (int)Convert.ToDouble(thisInputStats[i]);
            }
            // Set Output Info
            int[] OutputStats = new int[allData[4].Length];
            for (int i = 0; i < allData[4].Length; i++)
            {
                string[] thisOutputStats = allData[4];
                OutputStats[i] = (int)Convert.ToDouble(thisOutputStats[i]);
            }
            Neural_Network outputNetwork = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, 0);

            // Use data to set weights within network
            int currLayer = 0, currNode = 0;
            for (int i = 5; i < allData.Count; i++)             // the number of lines
            {
                for (int j = 0; j < allData[i].Length; j++)      // the number of weights in this node
                {
                    string[] weights = allData[i];
                    double weight = Convert.ToDouble(weights[j]);
                    outputNetwork.Layers[currLayer].Nodes[currNode].Weights[j] = weight;    // set weight
                }
                currNode++;
                if (currNode == outputNetwork.Layers[currLayer].Nodes.Length)
                {
                    currNode = 0;
                    currLayer++;
                }
            }

            return outputNetwork;
        }

        //
        // Reads in the contents of a .csv file to a string[] list
        public static List<string[]> ReadCSV(string file)
        {
            List<string[]> allData = new List<string[]>();
            using (TextFieldParser reader = new TextFieldParser(file))
            {
                reader.TextFieldType = FieldType.Delimited;
                reader.SetDelimiters(",");
                while (!reader.EndOfData)
                {
                    // Processing row
                    string[] fields = reader.ReadFields();
                    allData.Add(fields);
                }
            }
            return allData;
        }

        //
        // Trains a neural network via particle swarm
        public static Swarm RunParticleSwarm(ref Swarm swarm)
        {
            Stopwatch stopwatch = new Stopwatch(); ;
            Console.Write("Training neural network via particle swarm");
            for (int i = 1; i <= ITERATIONS; i++)
            {
                // Get start time
                if (i == 1)
                    stopwatch.Start();

                // Write progress
                if (i % (ITERATIONS / 10) == 0)
                {
                    Console.Write(100 * i / ITERATIONS);
                    if (i != ITERATIONS)
                        Console.Write(", ");
                    else
                        Console.WriteLine();
                }
                // Iterate
                swarm.GetParticleFitnesses(i);
                swarm.MoveParticles();
                if (swarm.AverageMovement.Last() < MIN_MOVEMENT)
                    swarm.ResetParticles();

                // Get stop time and estimate total runtime
                if (i == 5)
                {
                    stopwatch.Stop();
                    Console.WriteLine(", expected runtime: {0} s", ITERATIONS * stopwatch.ElapsedMilliseconds / 5000);
                }
            }
            Console.WriteLine();
            return swarm;
        }

        //
        // Trains a neural network using particle swarm
        public static Neural_Network TrainNetwork(double[] maximumData, Game[] Games, int[] LayerSizes, 
            bool[] inUseOpponent, bool[] inUseOffense, int[] inputStat, int[] outputStat,
            ref double[][] trainingInputData, ref double[][] trainingOutputData, ref double[][] testInputData, ref double[][] testOutputData, 
            string saveFile)
        {
            // Separate training and test data
            Console.WriteLine("Setting training and test data\n");
            SetTestTrainData(inUseOpponent, inUseOffense, inputStat, outputStat, ref trainingInputData, ref trainingOutputData,
                ref testInputData, ref testOutputData, ref maximumData, LayerSizes, Games);

            // Initialize and run swarm to get best network
            Console.WriteLine("Initializing swarm\n");
            Swarm swarm = new Swarm(N_PARTICLES, MOMENTUM, MAX_GLOBAL, MAX_PERSONAL,
                                    LayerSizes, inputStat, outputStat, inUseOpponent, inUseOffense, 
                                    trainingInputData, trainingOutputData, testInputData, testOutputData);
            RunParticleSwarm(ref swarm);
            Neural_Network oracle = swarm.bestNetwork;  // test performance of best network
            oracle.SaveNetwork(saveFile);               // save network


            // ================================================================================
            //   WRITE OUTPUT DATA  ===========================================================
            // ================================================================================

            // Output parameters
            string outputFileName = "../../Output/AllStats_Output.csv";
            StreamWriter outputFileStream = new StreamWriter(outputFileName);

            // Write neural network and/or swarm info
            string[] algorithmInfo = new string[3 + LayerSizes[LayerSizes.Length - 1]];
            algorithmInfo[0] = LayerSizes[0].ToString();
            algorithmInfo[1] = LayerSizes[LayerSizes.Length - 1].ToString();
            int maxCounter = 2;
            for (int i = 0; i < LayerSizes[LayerSizes.Length - 1]; i++)
            {
                int max = outputStat[i];
                algorithmInfo[maxCounter] = max.ToString();
                maxCounter++;
            }
            algorithmInfo[maxCounter] = NORMALIZE.ToString();
            for (int i = 0; i < algorithmInfo.Length; i++)
            {
                outputFileStream.Write(algorithmInfo[i]);
                if (i + 1 != algorithmInfo.Length)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();

            // Write movement data
            string[] movementOut = new string[swarm.AverageMovement.Count];
            for (int i = 0; i < swarm.AverageMovement.Count; i++)
            {
                movementOut[i] = swarm.AverageMovement[i].ToString();
                outputFileStream.Write(movementOut[i]);
                if (i + 1 != swarm.AverageMovement.Count)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();

            // Write global update value data
            string[] globalValOut = new string[swarm.GlobalBestHistory.Count];
            for (int i = 0; i < swarm.GlobalBestHistory.Count; i++)
            {
                globalValOut[i] = swarm.GlobalBestHistory[i].ToString();
                outputFileStream.Write(globalValOut[i]);
                if (i + 1 != swarm.GlobalBestHistory.Count)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();

            // Write global update iteration data
            string[] globalUpOut = new string[swarm.GlobalBestUpdateIteration.Count];
            for (int i = 0; i < swarm.GlobalBestUpdateIteration.Count; i++)
            {
                globalUpOut[i] = swarm.GlobalBestUpdateIteration[i].ToString();
                outputFileStream.Write(globalUpOut[i]);
                if (i + 1 != swarm.GlobalBestUpdateIteration.Count)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();

            // Write real output data
            string[] outRealString = new string[testOutputData.Length];
            for (int i = 0; i < testOutputData.Length; i++)
            {
                outRealString[i] = "";
                for (int j = 0; j < testOutputData[i].Length; j++)
                {
                    outRealString[i] += testOutputData[i][j].ToString();
                    if (j + 1 != testOutputData[i].Length)
                        outRealString[i] += ",";
                }
                outputFileStream.Write(outRealString[i]);
                if (i + 1 != testOutputData.Length)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();

            // Write neural network output data
            string[] outNNString = new string[testOutputData.Length];
            for (int i = 0; i < testOutputData.Length; i++)
            {
                double[] thisInput = testInputData[i];                      // get input for this output
                double[] thisOutput = swarm.bestNetwork.Think(thisInput);   // get network output
                outNNString[i] = "";
                for (int j = 0; j < thisOutput.Length; j++)     // put network outputs in outRealString[i]
                {
                    outNNString[i] += thisOutput[j].ToString();
                    if (j + 1 != thisOutput.Length)
                        outNNString[i] += ",";
                }
                outputFileStream.Write(outNNString[i]);
                if (i + 1 != testOutputData.Length)
                    outputFileStream.Write(",");
            }
            outputFileStream.WriteLine();
            outputFileStream.Close();   // close file

            return oracle;
        }

        //
        // Sets up input and output test and training data
        public static void SetTestTrainData(bool[] inUseOpponent, bool[] inUseOffense, int[] inputStat, int[] outputStat, 
            ref double[][] trainingInputData, ref double[][] trainingOutputData, ref double[][] testInputData, ref double[][] testOutputData,
            ref double[] maximumData, int[] LayerSizes, Game[] Games)
        {
            int nTrainingGames = (int)Math.Ceiling((1 - TEST_RATIO) * Games.Length);
            int nTestGames = (int)Math.Floor(TEST_RATIO * Games.Length);
            int randMax = (int)(1 / (1 - TEST_RATIO));
            Random random = new Random();
            int trainingCount = 0;
            int testCount = 0;
            for (int i = 0; i < Games.Length; i += (int)Math.Floor(1 / (1 - TEST_RATIO)))
            {
                // Get semi-random index of training game
                int thisGame = Convert.ToInt32(random.Next(randMax));
                while (thisGame + i >= Games.Length)
                    thisGame /= 2;

                // Set up training data row
                trainingInputData[trainingCount] = new double[LayerSizes[0]];
                trainingOutputData[trainingCount] = new double[LayerSizes[LayerSizes.Length - 1]];
                Game trainInputGame = Games[i + thisGame];  // training game

                // Get training teams
                Team trainHomeTeam = trainInputGame.HomeTeam;
                Team trainVisitorTeam = trainInputGame.VisitorTeam;

                // Enter input data
                for (int j = 0; j < LayerSizes[0]; j++)
                {
                    // choose using home or visiting team data
                    if (TRAIN_TYPE == HOME)
                    {
                        if (inUseOpponent[j])   // use this team's or opponent's stats
                        {
                            int type = inputStat[j];
                            bool offense = inUseOffense[j];
                            double gameStat = trainInputGame.VisitorData[inputStat[j]];
                            trainingInputData[trainingCount][j] = trainVisitorTeam.GetAverage(type, offense, trainInputGame);
                        }
                        else
                        {
                            int type = inputStat[j];
                            bool offense = inUseOffense[j];
                            double gameStat = trainInputGame.HomeData[inputStat[j]];
                            trainingInputData[trainingCount][j] = trainHomeTeam.GetAverage(type, offense, trainInputGame);
                        }
                    }
                    else if (TRAIN_TYPE == VISITOR)
                    {
                        if (inUseOpponent[j])   // use this team's or opponent's stats
                        {
                            int type = inputStat[j];
                            bool offense = inUseOffense[j];
                            double gameStat = trainInputGame.HomeData[inputStat[j]];
                            trainingInputData[trainingCount][j] = trainHomeTeam.GetAverage(type, offense, trainInputGame);
                        }
                        else
                        {
                            int type = inputStat[j];
                            bool offense = inUseOffense[j];
                            double gameStat = trainInputGame.VisitorData[inputStat[j]];
                            trainingInputData[trainingCount][j] = trainVisitorTeam.GetAverage(type, offense, trainInputGame);
                        }
                    }
                    else
                    {
                        // Use semi random home/visitor selection based on trainingCount
                        if (trainingCount % 2 == 0)     // use home
                        {
                            if (inUseOpponent[j])   // use this team's or opponent's stats
                            {
                                int type = inputStat[j];
                                bool offense = inUseOffense[j];
                                double gameStat = trainInputGame.VisitorData[inputStat[j]];
                                trainingInputData[trainingCount][j] = trainVisitorTeam.GetAverage(type, offense, trainInputGame);
                            }
                            else
                            {
                                int type = inputStat[j];
                                bool offense = inUseOffense[j];
                                double gameStat = trainInputGame.HomeData[inputStat[j]];
                                trainingInputData[trainingCount][j] = trainHomeTeam.GetAverage(type, offense, trainInputGame);
                            }
                        }
                        else                            // use visitor
                        {
                            if (inUseOpponent[j])   // use this team's or opponent's stats
                            {
                                int type = inputStat[j];
                                bool offense = inUseOffense[j];
                                double gameStat = trainInputGame.HomeData[inputStat[j]];
                                trainingInputData[trainingCount][j] = trainHomeTeam.GetAverage(type, offense, trainInputGame);
                            }
                            else
                            {
                                int type = inputStat[j];
                                bool offense = inUseOffense[j];
                                double gameStat = trainInputGame.VisitorData[inputStat[j]];
                                trainingInputData[trainingCount][j] = trainVisitorTeam.GetAverage(type, offense, trainInputGame);
                            }
                        }
                    }
                }

                // Manually enter output data
                if (TRAIN_TYPE == HOME)
                    trainingOutputData[trainingCount][0] = trainInputGame.HomeData[outputStat[0]];
                else if (TRAIN_TYPE == VISITOR)
                    trainingOutputData[trainingCount][0] = trainInputGame.VisitorData[outputStat[0]];
                else
                {
                    // Use semi random home/visitor selection based on trainingCount
                    if (trainingCount % 2 == 0)
                        trainingOutputData[trainingCount][0] = trainInputGame.HomeData[outputStat[0]];
                    else
                        trainingOutputData[trainingCount][0] = trainInputGame.VisitorData[outputStat[0]];
                }

                // Set up test data rows
                for (int j = 0; j < (int)Math.Floor(1 / (1 - TEST_RATIO)); j++)
                {
                    if (j != thisGame)
                    {
                        if (i + j >= Games.Length)
                            break;

                        int idx = (j < thisGame) ? (testCount + j) : (testCount + j - 1);
                        testInputData[idx] = new double[LayerSizes[0]];
                        testOutputData[idx] = new double[LayerSizes[LayerSizes.Length - 1]];
                        Game testInputGame = Games[i + j];  // test game

                        // Find test teams
                        Team testHomeTeam = testInputGame.HomeTeam;
                        Team testVisitorTeam = testInputGame.VisitorTeam;

                        // Enter input data
                        for (int k = 0; k < LayerSizes[0]; k++)
                        {
                            // choose using home or visiting team data
                            if (TRAIN_TYPE == HOME)
                            {
                                if (inUseOpponent[k])       // use this team's or opponent's stats
                                {
                                    int type = inputStat[k];
                                    bool offense = inUseOffense[k];
                                    double gameStat = testInputGame.VisitorData[inputStat[k]];
                                    testInputData[idx][k] = testVisitorTeam.GetAverage(type, offense, testInputGame);
                                }
                                else
                                {
                                    int type = inputStat[k];
                                    bool offense = inUseOffense[k];
                                    double gameStat = testInputGame.HomeData[inputStat[k]];
                                    testInputData[idx][k] = testHomeTeam.GetAverage(type, offense, testInputGame);
                                }
                            }
                            else if (TRAIN_TYPE == VISITOR)
                            {
                                if (inUseOpponent[k])       // use this team's or opponent's stats
                                {
                                    int type = inputStat[k];
                                    bool offense = inUseOffense[k];
                                    double gameStat = testInputGame.HomeData[inputStat[k]];
                                    testInputData[idx][k] = testHomeTeam.GetAverage(type, offense, testInputGame);
                                }
                                else
                                {
                                    int type = inputStat[k];
                                    bool offense = inUseOffense[k];
                                    double gameStat = testInputGame.VisitorData[inputStat[k]];
                                    testInputData[idx][k] = testVisitorTeam.GetAverage(type, offense, testInputGame);
                                }
                            }
                            else
                            {
                                // Use semi random home/visitor selection based on idx
                                if (idx % 2 == 0)           // use home
                                {
                                    if (inUseOpponent[k])   // use this team's or opponent's stats
                                    {
                                        int type = inputStat[k];
                                        bool offense = inUseOffense[k];
                                        double gameStat = testInputGame.VisitorData[inputStat[k]];
                                        testInputData[idx][k] = testVisitorTeam.GetAverage(type, offense, testInputGame);
                                    }
                                    else
                                    {
                                        int type = inputStat[k];
                                        bool offense = inUseOffense[k];
                                        double gameStat = testInputGame.HomeData[inputStat[k]];
                                        testInputData[idx][k] = testHomeTeam.GetAverage(type, offense, testInputGame);
                                    }
                                }
                                else                        // use visitor
                                {
                                    if (inUseOpponent[k])   // use this team's or opponent's stats
                                    {
                                        int type = inputStat[k];
                                        bool offense = inUseOffense[k];
                                        testInputData[idx][k] = testHomeTeam.GetAverage(type, offense, testInputGame);
                                    }
                                    else
                                    {
                                        int type = inputStat[k];
                                        bool offense = inUseOffense[k];
                                        testInputData[idx][k] = testVisitorTeam.GetAverage(type, offense, testInputGame);
                                    }
                                }
                            }
                        }

                        // Manually enter output data
                        if (TRAIN_TYPE == HOME)
                            testOutputData[idx][0] = testInputGame.HomeData[outputStat[0]];
                        else if (TRAIN_TYPE == VISITOR)
                            testOutputData[idx][0] = testInputGame.VisitorData[outputStat[0]];
                        else
                        {
                            // Use semi random home/visitor selection based on idx
                            if (idx % 2 == 0)
                                testOutputData[idx][0] = testInputGame.HomeData[outputStat[0]];
                            else
                                testOutputData[idx][0] = testInputGame.VisitorData[outputStat[0]];
                        }
                    }
                }
                trainingCount++;
                testCount += ((int)Math.Floor(1 / (1 - TEST_RATIO)) - 1);
            }

            // Normalize data
            NormalizeData(nTrainingGames, nTestGames, LayerSizes, inputStat, outputStat, maximumData, ref trainingInputData, ref trainingOutputData, ref testInputData, ref testOutputData);
        }
        
        //
        // Gets maximum data for normalization for all stats
        public static double[] FindMaximums(Game[] Games)
        {
            double[] maximumData = new double[Program.N_DATA_PTS];  // holds maximum values for each stat; for normalization
            for (int i = 0; i < Program.N_DATA_PTS; i++)
                maximumData[i] = 1;

            if (!Program.NORMALIZE)
                return maximumData;

            // Find maximums
            for (int i = 0; i < Games.Length; i++)
            {
                for (int j = 0; j < Program.N_DATA_PTS; j++)
                {
                    // List of stats not approved for normalizing
                    foreach (int stat in USE_GAME_STAT)
                        if (j == stat)
                            continue;

                    if (Games[i].HomeData[j] > maximumData[j])
                        maximumData[j] = Games[i].HomeData[j];

                    if (Games[i].VisitorData[j] > maximumData[j])
                        maximumData[j] = Games[i].VisitorData[j];
                }
            }
            return maximumData;
        }

        //
        // Normalizes data
        public static void NormalizeData(int nTrainingGames, int nTestGames, int[] LayerSizes, int[] inputStats, int[] outputStats, double[] maximumData, 
            ref double[][] trainingInputData, ref double[][] trainingOutputData, ref double[][] testInputData, ref double[][] testOutputData)
        {
            // Normalize training data
            for (int i = 0; i < nTrainingGames; i++)
            {
                for (int j = 0; j < LayerSizes[0]; j++)
                    trainingInputData[i][j] /= maximumData[inputStats[j]];   // input
                for (int j = 0; j < LayerSizes[LayerSizes.Length - 1]; j++)
                    trainingOutputData[i][j] /= maximumData[outputStats[j]]; // output
            }

            // Normalize test data
            for (int i = 0; i < nTestGames; i++)
            {
                for (int j = 0; j < LayerSizes[0]; j++) 
                    testInputData[i][j] /= maximumData[inputStats[j]];       // input
                for (int j = 0; j < LayerSizes[LayerSizes.Length - 1]; j++)
                    testOutputData[i][j] /= maximumData[outputStats[j]];     // output
            }
        }

        //
        // Gets the accuracy of one neural network
        public static double AnalyzeAccuracy(Game[] Games, int[] LayerSizes, bool[] inUseOpponent, bool[] inUseOffense, 
            int[] inputStat, int[] outputStat, double[] maximumData, Neural_Network oracle, ref int nGames, ref string[] predictions)
        {
            double correct = 0, MSE = 0, sumErr = 0;
            int invalid = 0;
            for (int i = 0; i < Games.Length; i++)
            {
                Team homeTeam = Games[i].HomeTeam;
                Team visitorTeam = Games[i].VisitorTeam;

                // Both teams must meet minimum game criteria
                if (homeTeam.Games.Length < 2 || visitorTeam.Games.Length < 2)
                {
                    nGames--;
                    invalid++;
                    continue;
                }

                // Enter inputs
                double[] inputsHome = new double[LayerSizes[0]];
                double[] inputsVisitor = new double[LayerSizes[0]];
                GetInputs(ref inputsHome, ref inputsVisitor, oracle, visitorTeam, homeTeam, maximumData, Games[i]);

                // Make prediction
                double[] homePtsArr = new double[LayerSizes.Last()];
                double[] visitorPtsArr = new double[LayerSizes.Last()];

                homePtsArr = oracle.Think(inputsHome);
                visitorPtsArr = oracle.Think(inputsVisitor);

                double homePts = homePtsArr[0] * maximumData[outputStat[0]];
                double visitorPts = visitorPtsArr[0] * maximumData[outputStat[0]];
                if (homePts > visitorPts && Games[i].HomeWin)
                    correct++;
                else if (homePts < visitorPts && Games[i].VisitorWin)
                    correct++;
                predictions[i - invalid] = homePts.ToString() + "," + visitorPts.ToString() + "," + Games[i].HomeData[oracle.OutputStats[0]] + "," + Games[i].VisitorData[oracle.OutputStats[0]];
                // Get errors
                double simDif = GetGameDifference(homePts, visitorPts, 0);
                double actDif = GetGameDifference(Games[i].HomeData[oracle.OutputStats[0]], Games[i].VisitorData[oracle.OutputStats[0]], 0);
                double absErr = Math.Abs(simDif - actDif);
                sumErr += absErr;
                MSE += absErr * absErr;
            }
            Console.WriteLine("Games: {0}", nGames);
            Console.WriteLine("Average error: {0}", sumErr / nGames);
            Console.WriteLine("Standard deviation: {0}", MSE / nGames);
            return correct;
        }
        // Gets the accuracy utilizing multiple networks
        public static double AnalyzeAccuracy(Game[] Games, double[] maximumData, Neural_Network[] networks, ref int nGames, 
            ref string[] predictions)
        {
            Console.WriteLine("Finding accuracy of network(s): ");
            double correct = 0, MSE = 0, sumErr = 0;
            int invalid = 0;
            for (int i = 0; i < Games.Length; i++)
            {
                // Update progress
                if (i % (Games.Length / 10) == 0)
                {
                    Console.Write("    {0}", Math.Ceiling(100 * (double)i / Games.Length));
                    if (i != Games.Length)
                        Console.WriteLine(" %");
                    else
                        Console.WriteLine();
                }

                Team homeTeam = Games[i].HomeTeam;
                Team visitorTeam = Games[i].VisitorTeam;

                // Both teams must meet minimum game criteria
                if (homeTeam.Games.Length < 2 || visitorTeam.Games.Length < 2)
                {
                    nGames--;
                    invalid++;
                    continue;
                }

                // Temp arrays for point differential (similar games)
                double[] homePtsArr = new double[networks[0].LayerSizes.Last()];
                double[] visitorPtsArr = new double[networks[0].LayerSizes.Last()];

                // Temp arrays for weighted average (similar games)
                double[] homePtsPredictions = new double[networks.Length];
                double[] visitorPtsPredictions = new double[networks.Length];
                double[][] networkErrArr = new double[networks.Length][];       // top row is home
                for (int j = 0; j < networks.Length; j++)                       // bottom is visitor
                    networkErrArr[j] = new double[4];

                // Save predictions for each array
                GetNetworkPredictions(ref homePtsPredictions, ref visitorPtsPredictions, ref networkErrArr, networks,
                    visitorTeam, homeTeam, maximumData, Games, Games[i]);

                // Predict outcome
                GetFinalPredictions(ref homePtsArr, ref visitorPtsArr, networks, homePtsPredictions, visitorPtsPredictions, networkErrArr);
                double homePts = homePtsArr[0] * maximumData[networks[0].OutputStats[0]];
                double visitorPts = visitorPtsArr[0] * maximumData[networks[0].OutputStats[0]];

                // Win-loss prediction and write to predictions array
                if (homePts > visitorPts && Games[i].HomeWin)
                    correct++;
                else if (homePts < visitorPts && Games[i].VisitorWin)
                    correct++;
                predictions[i - invalid] = homePts.ToString() + "," + visitorPts.ToString() + "," + Games[i].HomeData[networks[0].OutputStats[0]] + "," + Games[i].VisitorData[networks[0].OutputStats[0]];

                // Get errors
                double simDif = GetGameDifference(homePts, visitorPts, 0);
                double actDif = GetGameDifference(Games[i].HomeData[networks[0].OutputStats[0]], Games[i].VisitorData[networks[0].OutputStats[0]], 0);
                double absErr = Math.Abs(simDif - actDif);
                sumErr += absErr;
                MSE += absErr * absErr;
            }
            Console.WriteLine("\n");
            Console.WriteLine("Games: {0}", nGames);
            Console.WriteLine("Average error: {0}", sumErr / nGames);
            Console.WriteLine("Standard deviation: {0}", MSE / nGames);
            return correct;
        }

        //
        // Tries to predict a week. Uses only past game knowledge.
        public static double SimulateWeek(Season season, int[] dates, Neural_Network[] networks, ref string[] predictions, 
            ref int nGames, Game[] similarGames)
        {
            Console.WriteLine("Finding accuracy of network(s): ");
            Console.WriteLine("          Start date: {0}", dates[0]);
            Console.WriteLine("          Final date: {0}", dates[1]);
            double correct = 0, MSE = 0, sumErr = 0;
            List<string> predictionsList = new List<string>();

            // Get games
            Game[] Games = GetGoodGames(season, dates);
            nGames = Games.Length;

            // Get training maximums
            double[] maximumData = FindMaximums(Games);

            // Find accuracy of each game
            for (int i = 0; i < Games.Length; i++)
            {
                // Update progress
                if (i % (Games.Length / 10) == 0)
                {
                    Console.Write("    {0}", Math.Ceiling(100 * (double)i / Games.Length));
                    if (i != Games.Length)
                        Console.WriteLine(" %");
                    else
                        Console.WriteLine();
                }

                Team homeTeam = Games[i].HomeTeam;
                Team visitorTeam = Games[i].VisitorTeam;

                // Temp arrays for point differential (similar games)
                double[] homePtsArr = new double[networks[0].LayerSizes.Last()];
                double[] visitorPtsArr = new double[networks[0].LayerSizes.Last()];

                // Temp arrays for weighted average (similar games)
                double[] homePtsPredictions = new double[networks.Length];
                double[] visitorPtsPredictions = new double[networks.Length];
                double[][] networkErrArr = new double[networks.Length][];       // top row is home
                for (int j = 0; j < networks.Length; j++)                       // bottom is visitor
                    networkErrArr[j] = new double[4];

                // Save predictions for each array
                GetNetworkPredictions(ref homePtsPredictions, ref visitorPtsPredictions, ref networkErrArr, networks,
                    visitorTeam, homeTeam, maximumData, similarGames, Games[i]);

                // Predict outcome
                GetFinalPredictions(ref homePtsArr, ref visitorPtsArr, networks, homePtsPredictions, visitorPtsPredictions, networkErrArr);
                double homePts = homePtsArr[0] * maximumData[networks[0].OutputStats[0]];
                double visitorPts = visitorPtsArr[0] * maximumData[networks[0].OutputStats[0]];

                // Win-loss prediction and write to predictions array
                if (homePts > visitorPts && Games[i].HomeWin)
                    correct++;
                else if (homePts < visitorPts && Games[i].VisitorWin)
                    correct++;
                if (Double.IsNaN(homePts) || Double.IsNaN(visitorPts))
                    continue;

                predictionsList.Add(homePts.ToString() + "," + visitorPts.ToString() + "," 
                    + Games[i].HomeData[networks[0].OutputStats[0]] + "," + Games[i].VisitorData[networks[0].OutputStats[0]] + ","
                    + Games[i].HomeTeam.TeamName + "," + Games[i].VisitorTeam.TeamName);

                // Get errors
                double simDif = GetGameDifference(homePts, visitorPts, 0);
                double actDif = GetGameDifference(Games[i].HomeData[networks[0].OutputStats[0]], Games[i].VisitorData[networks[0].OutputStats[0]], 0);
                double absErr = Math.Abs(simDif - actDif);
                sumErr += absErr;
                MSE += absErr * absErr;
            }
            Console.WriteLine("\n");
            Console.WriteLine("Games: {0}", Games.Length);
            Console.WriteLine("Average error: {0}", sumErr / Games.Length);
            Console.WriteLine("Standard deviation: {0}", MSE / Games.Length);

            predictions = predictionsList.ToArray();

            return correct;
        }

        //
        // Returns an array of predictions for each network
        public static void GetNetworkPredictions(ref double[] homePtsPredictions, ref double[] visitorPtsPredictions, 
            ref double[][] networkErrArr, Neural_Network[] networks, Team vTeam, Team hTeam, double[] maxes, Game[] Games, 
            Game game)
        {
            int currNetwork = 0;
            foreach (Neural_Network network in networks)
            {
                // Make sure each network has same number of outputs
                if (network.LayerSizes.Last() != networks[0].LayerSizes.Last())
                {
                    Console.WriteLine("ERROR: Output of LayerSizes do not agree in AnalyzeAccuracy()");
                    return;
                }

                // Enter inputs
                double[] inputsHome = new double[network.LayerSizes[0]];
                double[] inputsVisitor = new double[network.LayerSizes[0]];
                GetInputs(ref inputsHome, ref inputsVisitor, network, vTeam, hTeam, maxes, game);

                // Make prediction
                double[] tmpHomePtsArr = new double[1];
                double[] tmpVisitorPtsArr = new double[1];

                // Get and save prediction
                tmpHomePtsArr = network.Think(inputsHome);
                tmpVisitorPtsArr = network.Think(inputsVisitor);
                homePtsPredictions[currNetwork] = tmpHomePtsArr[0];
                visitorPtsPredictions[currNetwork] = tmpVisitorPtsArr[0];

                // Get and save network accuracy for similar games
                if (Program.PREDICTION_TYPE == Program.SIM_AVG)
                {
                    Game[] SimilarGames = FindSimilarGames(Games, network.InputStats, network.LayerSizes, inputsHome, inputsVisitor, 
                        network.UseOpponent, network.UseOffense, maxes, Program.N_SIM_GAMES, game.GameCode);
                    networkErrArr[currNetwork] = GetNetworkError(SimilarGames, network, maxes);
                }
                currNetwork++;
            }
        }

        //
        // Combines network predictions to form a final output
        public static void GetFinalPredictions(ref double[] homePtsArr, ref double[] visitorPtsArr, Neural_Network[] networks,
            double[] homePtsPredictions, double[] visitorPtsPredictions, double[][] networkErrArr)
        {
            // Use network with greatest differential
            if (Program.PREDICTION_TYPE == Program.DIF)
            {
                double dif = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    // Get biggest difference (FINAL)
                    double tmpDif = Math.Abs(homePtsPredictions[j] - visitorPtsPredictions[j]);
                    if (tmpDif > dif)
                    {
                        dif = tmpDif;
                        homePtsArr[0] = homePtsPredictions[j];
                        visitorPtsArr[0] = visitorPtsPredictions[j];
                    }
                }
            }
            // Use weighted average of networks based on their accuracy of similar games
            else if (Program.PREDICTION_TYPE == Program.SIM_AVG)
            {
                // Get error sum
                double[] networkErrSum = new double[2];
                networkErrSum[0] = 0;
                networkErrSum[1] = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    networkErrSum[0] += networkErrArr[j][0];    // home
                    networkErrSum[1] += networkErrArr[j][1];    // visitor
                }

                // Normalize by the sum
                for (int j = 0; j < networks.Length; j++)
                {
                    networkErrArr[j][0] /= networkErrSum[0];    // home
                    networkErrArr[j][1] /= networkErrSum[1];    // visitor
                }

                // Get factor total
                double[] factorTotal = new double[2];
                for (int j = 0; j < factorTotal.Length; j++)
                    factorTotal[j] = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    double factor = networkErrSum[0] / networkErrArr[j][0]; // Get home factor
                    factorTotal[0] += factor;
                    factor = networkErrSum[1] / networkErrArr[j][1];        // Get visitor factor
                    factorTotal[1] += factor;
                }

                // Get weighted percentages
                double[][] percentages = new double[networks.Length][];
                for (int j = 0; j < percentages.Length; j++)
                    percentages[j] = new double[2];
                for (int j = 0; j < networks.Length; j++)
                {
                    double factor = networkErrSum[0] / networkErrArr[j][0]; // Home %
                    percentages[j][0] = factor / factorTotal[0];
                    factor = networkErrSum[1] / networkErrArr[j][1];        // Visitor %
                    percentages[j][1] = factor / factorTotal[1];
                }

                // Get biases
                double[] networkErrBias = new double[2];
                networkErrBias[0] = 0;
                networkErrBias[1] = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    networkErrBias[0] += networkErrArr[j][2] * percentages[j][0];       // home
                    networkErrBias[1] += networkErrArr[j][3] * percentages[j][1];       // visitor
                }

                // Get weighted average (FINAL)
                homePtsArr[0] = 0;
                visitorPtsArr[0] = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    homePtsArr[0] += percentages[j][0] * homePtsPredictions[j];
                    visitorPtsArr[0] += percentages[j][1] * visitorPtsPredictions[j];
                }
            }
            // Use weighted average dependent on the differential in scoring
            else if (Program.PREDICTION_TYPE == Program.PTS_AVG)
            {
                // Get difference sum
                double networkDifSum = 0;
                for (int j = 0; j < networks.Length; j++)
                    networkDifSum += Math.Abs(homePtsPredictions[j] - visitorPtsPredictions[j]);

                // Get percentage array
                double[] percentages = new double[networks.Length];
                for (int j = 0; j < networks.Length; j++)
                    percentages[j] = Math.Abs(homePtsPredictions[j] - visitorPtsPredictions[j]) / networkDifSum;

                // Get weighted average (FINAL)
                homePtsArr[0] = 0;
                visitorPtsArr[0] = 0;
                for (int j = 0; j < networks.Length; j++)
                {
                    homePtsArr[0] += percentages[j] * homePtsPredictions[j];
                    visitorPtsArr[0] += percentages[j] * visitorPtsPredictions[j];
                }
            }
        }

        //
        // Returns an array of games that are within the acceptable date
        public static Game[] GetGoodGames(Season season, int[] dates)
        {
            List<Game> goodGames = new List<Game>();
            Game[] allGames = season.Games;

            foreach (Game G in allGames)
            {
                // Only get FBS games
                if (G.HomeTeam.Conf.Division == "FCS" || G.VisitorTeam.Conf.Division == "FCS")
                    continue;

                // Gets rid of teams in the FCS last year (Georgia State)
                if (G.HomeTeam.PastSeasons.Count > 0 && G.VisitorTeam.PastSeasons.Count > 0)
                    if (G.HomeTeam.PastSeasons[0].Teams[0].Conf.Division == "FCS" || G.VisitorTeam.PastSeasons[0].Teams[0].Conf.Division == "FCS")
                        continue;

                // Too early or too late
                if (G.Date < dates[0] || G.Date >= dates[1])
                    continue;

                // Good to go
                goodGames.Add(G);
            }

            return goodGames.ToArray();
        }

        //
        // Gets input arrays for both teams' stats to a certain date
        public static void GetInputs(ref double[] hInputs, ref double[] vInputs, Neural_Network network, Team vTeam, Team hTeam, double[] maxes, Game game)
        {
            for (int j = 0; j < network.LayerSizes[0]; j++)
            {
                // Opponent stat?
                if (network.UseOpponent[j])
                {
                    int inputStat = network.InputStats[j];
                    bool offense = network.UseOffense[j];
                    hInputs[j] = vTeam.GetAverage(inputStat, offense, game) / maxes[inputStat];
                    vInputs[j] = hTeam.GetAverage(inputStat, offense, game) / maxes[inputStat];
                }
                else
                {
                    int inputStat = network.InputStats[j];
                    bool offense = network.UseOffense[j];
                    hInputs[j] = hTeam.GetAverage(inputStat, offense, game) / maxes[inputStat];
                    vInputs[j] = vTeam.GetAverage(inputStat, offense, game) / maxes[inputStat];
                }
            }
        }

        //
        // Returns the error of game predictions by a network. The first two values are MSE, the second two are biases.
        public static double[] GetNetworkError(Game[] Games, Neural_Network network, double[] maximumData)
        {
            double[] mse = new double[4];
            for (int i = 0; i < mse.Length; i++)
                mse[i] = 0;

            foreach (Game G in Games)
            {
                Team visitorTeam = G.VisitorTeam;
                Team homeTeam = G.HomeTeam;

                // Enter inputs
                double[] inputsHome = new double[network.LayerSizes[0]];
                double[] inputsVisitor = new double[network.LayerSizes[0]];
                for (int j = 0; j < network.LayerSizes[0]; j++)
                {
                    // Opponent stat?
                    if (network.UseOpponent[j])
                    {
                        int inputStat = network.InputStats[j];
                        bool offense = network.UseOffense[j];
                        inputsHome[j] = visitorTeam.GetAverage(inputStat, offense, G) / maximumData[inputStat];
                        inputsVisitor[j] = homeTeam.GetAverage(inputStat, offense, G) / maximumData[inputStat];
                    }
                    else
                    {
                        int inputStat = network.InputStats[j];
                        bool offense = network.UseOffense[j];
                        inputsHome[j] = homeTeam.GetAverage(inputStat, offense, G) / maximumData[inputStat];
                        inputsVisitor[j] = visitorTeam.GetAverage(inputStat, offense, G) / maximumData[inputStat];
                    }
                }

                // Get Predictions
                double[] homePredictionArr = network.Think(inputsHome);
                double[] visitorPredictionArr = network.Think(inputsVisitor);
                double homePrediction = homePredictionArr[0] * maximumData[network.OutputStats[0]];
                double visitorPrediction = visitorPredictionArr[0] * maximumData[network.OutputStats[0]];

                // Find difference and get MSE
                double homeDif = homePrediction - G.HomeData[network.OutputStats[0]];
                double visitorDif = visitorPrediction - G.VisitorData[network.OutputStats[0]];

                // Mitigate error that is correct
                if ((homePrediction > visitorPrediction && G.HomeWin) || (homePrediction < visitorPrediction && G.VisitorWin))
                {
                    homeDif /= CORRECT_FACTOR;
                    visitorDif /= CORRECT_FACTOR;
                }
                else
                {
                    homeDif *= CORRECT_FACTOR;
                    visitorDif *= CORRECT_FACTOR;
                }
                mse[0] += (homeDif * homeDif);
                mse[1] += (visitorDif * visitorDif);
                mse[2] += homeDif;
                mse[3] += visitorDif;
            }
            mse[0] /= Games.Length;
            mse[1] /= Games.Length;
            mse[2] /= Games.Length;
            mse[3] /= Games.Length;

            return mse;
        }

        //
        // Returns x games with the most similar inputs
        public static Game[] FindSimilarGames(Game[] Games, int[] inputStat, int[] LayerSizes, double[] homeArray, double[] visitorArray,
            bool[] inUseOpponent, bool[] inUseOffense, double[] maximumData, int nGames, long gameCode)
        {
            Game[] returnGames = new Game[nGames];
            double[] gameMSEs = new double[nGames];
            for (int i = 0; i < gameMSEs.Length; i++)
                gameMSEs[i] = int.MaxValue;

            int game = 0, returnGameLen = 0;
            foreach (Game G in Games)
            {
                // Don't find the same game
                if (G.GameCode == gameCode || G.HomeTeam.Games.Length < 2 || G.VisitorTeam.Games.Length < 2)
                    continue;

                // Can't already be in the array
                bool done = false;
                for (int i = 0; i < returnGameLen; i++)
                    if (returnGames[i].GameCode == G.GameCode)
                        done = true;
                if (done)
                    continue;

                double[] newHomeArr = new double[LayerSizes[0]];
                double[] newVisitorArr = new double[LayerSizes[0]];
                for (int j = 0; j < LayerSizes[0]; j++)
                {
                    // Opponent stat?
                    if (inUseOpponent[j])
                    {
                        int type = inputStat[j];
                        bool offense = inUseOffense[j];
                        newHomeArr[j] = G.VisitorTeam.GetAverage(type, offense, G) / maximumData[inputStat[j]];
                        newVisitorArr[j] = G.HomeTeam.GetAverage(type, offense, G) / maximumData[inputStat[j]];
                    }
                    else
                    {
                        int type = inputStat[j];
                        bool offense = inUseOffense[j];
                        newHomeArr[j] = G.HomeTeam.GetAverage(type, offense, G) / maximumData[inputStat[j]];
                        newVisitorArr[j] = G.VisitorTeam.GetAverage(type, offense, G) / maximumData[inputStat[j]];
                    }
                }

                // Find this game's place in the array
                double thisMSE = GetArrayMSE(newHomeArr, homeArray) + GetArrayMSE(newVisitorArr, visitorArray);
                int pos = nGames;
                while (gameMSEs[pos - 1] > thisMSE)
                {
                    pos--;
                    if (pos == 0)
                        break;
                }
                if (pos < nGames)
                {
                    // Shift everything else so nothing is overwritten
                    for (int i = nGames - 1; i > pos; i--)
                    {
                        gameMSEs[i] = gameMSEs[i - 1];
                        returnGames[i] = returnGames[i - 1];
                    }
                    gameMSEs[pos] = thisMSE;
                    returnGames[pos] = G;
                    if (returnGameLen + 1 < returnGames.Length)
                        returnGameLen++;
                }
                game++;
            }

            return returnGames;
        }

        //
        // Gets the mean-squared error between two sets of arrays
        public static double GetArrayMSE(double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length)
            {
                Console.WriteLine("ERROR: arrays are not the same size in GetArrayMSE");
                return 0;
            }

            double MSE = 0;
            for (int i = 0; i < array1.Length; i++)
                MSE += Math.Abs(array1[i] - array2[i]) * Math.Abs(array1[i] - array2[i]);

            return MSE / array1.Length;
        }

        //
        // Returns the game differential with a cap on score for blowouts
        public static double GetGameDifference(double homeScore, double visitorScore)
        {
            double origDif = homeScore - visitorScore;
            if (origDif < 0)
                return origDif > DIF_CAP ? origDif : -1 * DIF_CAP;
            else if (origDif > 0)
                return origDif < DIF_CAP ? origDif : DIF_CAP;
            else
                return origDif;
        }
        // Option to set a manual cap
        public static double GetGameDifference(double homeScore, double visitorScore, double manCap)
        {
            double origDif = homeScore - visitorScore;
            if (manCap == 0)
                return origDif;

            if (origDif < 0)
                return origDif > manCap ? origDif : -1 * manCap;
            else if (origDif > 0)
                return origDif < manCap ? origDif : manCap;
            else
                return origDif;
        }
    }
}