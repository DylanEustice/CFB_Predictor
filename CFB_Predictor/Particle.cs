using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Particle
    {
        public double ParticleFitness;
        public double PersonalBest = double.MaxValue;
        public Neural_Network bestNetwork;
        public Neural_Network currNetwork;
        public Neural_Network stepNetwork;
        public double AverageMovement = double.MaxValue;

        //
        // Constructor
        public Particle(int[] LayerInfo, int[] inputStats, int[] outputStats, bool[] inUseOpponent, bool[] inUseOffense, Random random)
        {
            currNetwork = new Neural_Network(LayerInfo, inputStats, outputStats, inUseOpponent, inUseOffense, random);
            bestNetwork = currNetwork.SetWeights();
            stepNetwork = new Neural_Network(LayerInfo, inputStats, outputStats, inUseOpponent, inUseOffense, 0);
        }

        //
        // Adjusts the weights of the particle
        public void MoveParticle(double momentum, double globalWeight, double personalWeight, Neural_Network globalBestNetwork)
        {
            // Apply momentum to the last step
            Neural_Network step;
            step = stepNetwork.MultiplyConstant(momentum);

            // Add difference between current weights and group best
            Neural_Network tmpG = globalBestNetwork.SubtractNetworks(currNetwork);
            tmpG.MultiplyConstant(globalWeight);
            step.AddNetwork(tmpG);

            // Add difference between current weights and personal best
            Neural_Network tmpP = bestNetwork.SubtractNetworks(currNetwork);
            tmpP.MultiplyConstant(globalWeight);
            step.AddNetwork(tmpP);

            // Keep movement small
            if (Program.MAX_MOVEMENT > 0)
                while (step.AverageWeights() > Program.MAX_MOVEMENT)
                    step = step.MultiplyConstant(0.5);

            // Add to the current weights and set as last step
            currNetwork.AddNetwork(step);
            stepNetwork = step.SetWeights();
            
            // Find average movement in this step
            AverageMovement = stepNetwork.AverageWeights();
        }

        //
        // Find fitness of this particle for all training data
        //  - This is the summed squared error
        public double GetFitness(double[][] inputArray, double[][] outputArray)
        {
            double error = 0;   // Initialize error to 0

            // Get error
            int row = 0;
            foreach (double[] line in inputArray)
            {
                double[] output = currNetwork.Think(line);
                int col = 0;
                foreach (double val in output)
                {
                    error += Math.Abs((val - outputArray[row][col]) * (val - outputArray[row][col]));
                    col++;
                }
                row++;
            }
            ParticleFitness = error;

            // Check for personal best
            if (ParticleFitness < PersonalBest)
            {
                PersonalBest = ParticleFitness;
                bestNetwork = currNetwork.SetWeights();
            }

            return ParticleFitness;
        }
    }
}