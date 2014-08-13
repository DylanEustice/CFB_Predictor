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
    public class Neural_Network
    {
        public Layer[] Layers;
        public int[] LayerSizes;
        public bool[] UseOpponent;
        public bool[] UseOffense;
        public int[] InputStats;
        public int[] OutputStats;

        //
        // Constructor
        public Neural_Network(int[] LayerInfo, int[] inputStats, int[] outputStats, bool[] inUseOpponent, bool[] inUseOffense, Random random)
        {
            // Initialize layers
            LayerSizes = LayerInfo;
            InputStats = inputStats;
            OutputStats = outputStats;
            UseOpponent = inUseOpponent;
            UseOffense = inUseOffense;
            Layers = new Layer[LayerSizes.Length];
            for (int i = 0; i < LayerSizes.Length - 1; i++)
                Layers[i] = new Layer(LayerSizes[i], LayerSizes[i + 1], random);
            Layers[LayerSizes.Length - 1] = new Layer(LayerSizes[LayerSizes.Length - 1], true);
        }
        public Neural_Network(int[] LayerInfo, int[] inputStats, int[] outputStats, bool[] inUseOpponent, bool[] inUseOffense, Random random, int resets)
        {
            // Initialize layers
            LayerSizes = LayerInfo;
            InputStats = inputStats;
            OutputStats = outputStats;
            UseOpponent = inUseOpponent;
            UseOffense = inUseOffense;
            Layers = new Layer[LayerSizes.Length];
            for (int i = 0; i < LayerSizes.Length - 1; i++)
                Layers[i] = new Layer(LayerSizes[i], LayerSizes[i + 1], random, resets);
            Layers[LayerSizes.Length - 1] = new Layer(LayerSizes[LayerSizes.Length - 1], true);
        }
        // Initializes layers with a specific weight
        public Neural_Network(int[] LayerInfo, int[] inputStats, int[] outputStats, bool[] inUseOpponent, bool[] inUseOffense, double weightInit)
        {
            // Initialize layers
            LayerSizes = LayerInfo;
            InputStats = inputStats;
            OutputStats = outputStats;
            UseOpponent = inUseOpponent;
            UseOffense = inUseOffense;
            Layers = new Layer[LayerSizes.Length];
            for (int i = 0; i < LayerSizes.Length - 1; i++)
                Layers[i] = new Layer(LayerSizes[i], LayerSizes[i + 1], weightInit);
            Layers[LayerSizes.Length - 1] = new Layer(LayerSizes[LayerSizes.Length - 1], true);
        }

        //
        // Prints out the weights of the neural network
        public void Print()
        {
            Console.WriteLine("== NEURAL NETWORK ====================\n");
            for (int i = 1; i <= LayerSizes.Length; i++)
                Layers[i - 1].Print(i);
            Console.WriteLine("======================================\n");
        }

        //
        // Finds the output of a neural network given a set of inputs
        public double[] Think(double[] rawInputs)
        {
            double[] inputs;
            if (Program.USE_BIAS > 0)
            {
                List<double> inWithBias = new List<double>();
                foreach (double i in rawInputs)
                    inWithBias.Add(i);
                inWithBias.Add(Program.USE_BIAS);
                inputs = inWithBias.ToArray();
            }
            else 
            {
                inputs = rawInputs;
            }

            // Set the inputs to the first layer
            for (int i = 0; i < inputs.Length; i++)
                Layers[0].Nodes[i].Value = inputs[i];

            // Find values in all lower layers
            for (int i = 1; i < LayerSizes.Length; i++)
            {
                int limitNodes;
                if (Program.USE_BIAS > 0 && Layers[i].Nodes.Length > 1)
                    limitNodes = Layers[i].Nodes.Length - 1;
                else
                    limitNodes = Layers[i].Nodes.Length;

                for (int j = 0; j < limitNodes; j++)
                {
                    Layers[i].Nodes[j].Value = 0;
                    for (int k = 0; k < Layers[i - 1].Nodes.Length; k++)
                    {
                        double nodeValue = Layers[i - 1].Nodes[k].Value;
                        double nodeWeight = Layers[i - 1].Nodes[k].Weights[j];
                        Layers[i].Nodes[j].Value += nodeValue * nodeWeight;
                    }

                    // Include bias node
                    if (i + 1 < LayerSizes.Length)
                        Layers[i].Nodes[Layers[i].Nodes.Length - 1].Value = Program.USE_BIAS;
                }
            }
            // Get output
            double[] output = new double[LayerSizes.Last()];
            int count = 0;
            foreach (Node n in Layers[Layers.Length - 1].Nodes)
            {
                output[count] = n.Value;
                count++;
            }
            return output;
        }

        //
        // Multiplies the neural network by a constant value
        public Neural_Network MultiplyConstant(double val)
        {
            Neural_Network output = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, 0);
            for (int i = 0; i < Layers.Length; i++)
                for (int j = 0; j < Layers[i].Nodes.Length; j++)
                    for (int k = 0; k < Layers[i].Nodes[j].Weights.Length; k++)
                        output.Layers[i].Nodes[j].Weights[k] = val * Layers[i].Nodes[j].Weights[k];
            return output;
        }

        //
        // Adds the values of another network to this
        public void AddNetwork(Neural_Network network)
        {
            for (int i = 0; i < Layers.Length; i++)
                for (int j = 0; j < Layers[i].Nodes.Length; j++)
                    for (int k = 0; k < Layers[i].Nodes[j].Weights.Length; k++)
                        Layers[i].Nodes[j].Weights[k] += network.Layers[i].Nodes[j].Weights[k];
        }

        //
        // Subtracts another network from this one, returning the output but not changing this
        public Neural_Network SubtractNetworks(Neural_Network network)
        {
            Neural_Network output = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, 0);
            for (int i = 0; i < Layers.Length; i++)
                for (int j = 0; j < Layers[i].Nodes.Length; j++)
                    for (int k = 0; k < Layers[i].Nodes[j].Weights.Length; k++)
                        output.Layers[i].Nodes[j].Weights[k] = Layers[i].Nodes[j].Weights[k] - network.Layers[i].Nodes[j].Weights[k];
            return output;
        }

        //
        // Sums returns the average value of the weights in this network
        public double AverageWeights()
        {
            double tot = 0, weights = 0;
            for (int i = 0; i < Layers.Length; i++)
                for (int j = 0; j < Layers[i].Nodes.Length; j++)
                    for (int k = 0; k < Layers[i].Nodes[j].Weights.Length; k++)
                    {
                        tot += Math.Abs(Layers[i].Nodes[j].Weights[k]);
                        weights++;
                    }
            return tot / weights;
        }


        //
        // Sums returns the average value of the weights in this network
        public Neural_Network SetWeights()
        {
            Neural_Network output = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, 0);
            for (int i = 0; i < Layers.Length; i++)
                for (int j = 0; j < Layers[i].Nodes.Length; j++)
                    for (int k = 0; k < Layers[i].Nodes[j].Weights.Length; k++)
                        output.Layers[i].Nodes[j].Weights[k] = Layers[i].Nodes[j].Weights[k];
            return output;
        }

        //
        // Saves the network to a specified file
        // "Layer Sizes"            "4 , 3 , 2 , 1"
        // "Use Opponent"           "true , false , false , true"
        // "Use Offense"            "false , true , false , true"
        // "Input Types"            "4 , 45 , 23 , 15"
        // "Ouptut Types"           "40"
        // "j^th node weights"      "0.2 , 0.3 , 0.4 , 0.9"
        // "j+1^th node weights"    "0.2 , 0.3 , 0.4 , 0.9"
        // "..."
        // "M^th node weights"      "0.5"
        public void SaveNetwork(string file)
        {
            // Determine how many lines are needed to save this network
            int linesNeeded = 5;
            foreach (Layer L in Layers)
                linesNeeded += L.Nodes.Length;
            string[] network = new string[linesNeeded];
            
            // Write layer sizes to top line
            network[0] = "";
            for (int i = 0; i < LayerSizes.Length; i++)
            {
                int size = LayerSizes[i];
                if (i + 1 == LayerSizes.Length)
                    network[0] += size.ToString();
                else
                    network[0] += size.ToString() + ",";
            }

            // Write opponent data info
            network[1] = "";
            for (int i = 0; i < UseOpponent.Length; i++)
            {
                bool opp = UseOpponent[i];
                if (i + 1 == UseOpponent.Length)
                    network[1] += opp.ToString();
                else
                    network[1] += opp.ToString() + ",";
            }

            // Write offense data info
            network[2] = "";
            for (int i = 0; i < UseOffense.Length; i++)
            {
                bool off = UseOffense[i];
                if (i + 1 == UseOffense.Length)
                    network[2] += off.ToString();
                else
                    network[2] += off.ToString() + ",";
            }

            // Write input data type
            network[3] = "";
            for (int i = 0; i < InputStats.Length; i++)
            {
                int inStat = InputStats[i];
                if (i + 1 == InputStats.Length)
                    network[3] += inStat.ToString();
                else
                    network[3] += inStat.ToString() + ",";
            }

            // Write output data type
            network[4] = "";
            for (int i = 0; i < OutputStats.Length; i++)
            {
                int outStat = OutputStats[i];
                if (i + 1 == OutputStats.Length)
                    network[4] += outStat.ToString();
                else
                    network[4] += outStat.ToString() + ",";
            }

            // Convert all weights to strings and add to network
            int currLine = 5;
            foreach (Layer L in Layers)
            {
                foreach (Node N in L.Nodes)
                {
                    // Add this weight to the line
                    network[currLine] = "";
                    for (int i = 0; i < N.Weights.Length; i++)
                    {
                        double weight = N.Weights[i];
                        if (i + 1 == N.Weights.Length)
                            network[currLine] += weight.ToString();
                        else
                            network[currLine] += weight.ToString() + ",";
                    }
                    currLine++;     // increment line number
                }
            }

            // Write network to file
            string path = "../../Networks/";
            using (StreamWriter writer = new StreamWriter(path + file))
                foreach (string line in network)
                    writer.WriteLine(line);
        }
    }
}