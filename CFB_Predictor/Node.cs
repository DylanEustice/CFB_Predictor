using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Node
    {
        public double Value;
        public double[] Weights;

        //
        // Constructor
        public Node(int nWeights)
        {
            Value = 0;
            Weights = new double[nWeights];
        }
    }
}