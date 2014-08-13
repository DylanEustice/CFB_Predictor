using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Layer
    {
        public Node[] Nodes;
        public bool isOutput = false;

        //
        // Constructor
        public Layer(int LayerSize, int ChildSize, Random random)
        {
            if (Program.USE_BIAS > 0)
                Nodes = new Node[LayerSize + 1];
            else
                Nodes = new Node[LayerSize];

            for (int i = 0; i < Nodes.Length; i++)
                Nodes[i] = new Node(ChildSize);

            // Assign random weights to each node
            foreach (Node n in Nodes)
            {
                for (int i = 0; i < n.Weights.Length; i++)
                {
                    double weight = 2 * Program.MAX_WEIGHT * random.NextDouble() - Program.MIN_WEIGHT;
                    n.Weights[i] = weight;
                }
            }
        }
        public Layer(int LayerSize, int ChildSize, Random random, int resets)
        {
            if (Program.USE_BIAS > 0)
                Nodes = new Node[LayerSize + 1];
            else
                Nodes = new Node[LayerSize];

            for (int i = 0; i < Nodes.Length; i++)
                Nodes[i] = new Node(ChildSize);

            // Assign random weights to each node
            foreach (Node n in Nodes)
            {
                for (int i = 0; i < n.Weights.Length; i++)
                {
                    double weight = 2 * (Program.MAX_WEIGHT + Program.RESET_WEIGHT * resets) * random.NextDouble() - (Program.MAX_WEIGHT + Program.RESET_WEIGHT * resets);
                    n.Weights[i] = weight;
                }
            }
        }
        // Assigns a specified value to weights to the nodes
        public Layer(int LayerSize, int ChildSize, double weightInit)
        {
            if (Program.USE_BIAS > 0)
                Nodes = new Node[LayerSize + 1];
            else
                Nodes = new Node[LayerSize];

            for (int i = 0; i < Nodes.Length; i++)
                Nodes[i] = new Node(ChildSize);

            // Assign weights 
            foreach (Node n in Nodes)
                for (int i = 0; i < n.Weights.Length; i++)
                    n.Weights[i] = weightInit;
        }
        // The output layer
        public Layer(int LayerSize, bool last)
        {
            Nodes = new Node[LayerSize];
            isOutput = last;
            for (int i = 0; i < LayerSize; i++)
                Nodes[i] = new Node(0);
        }

        //
        // Prints out the weights in this layer and recurses if not the last layer
        public void Print(int loc)
        {
            Console.WriteLine("  -- Layer: {0} ---------------\n", loc);
            int i = 0;
            foreach (Node n in Nodes)
            {
                i++;
                Console.WriteLine("  Node: {0} - [{1:N2}]", i, n.Value);
                foreach (double w in n.Weights)
                    Console.WriteLine("    {0:N2}", w);
                Console.WriteLine();
            }
        }
    }
}