using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFB_Predictor
{
    public class Swarm
    {
        public double GlobalBest = double.MaxValue;
        public List<double> GlobalBestHistory = new List<double>();
        public List<double> GlobalBestUpdateIteration = new List<double>();
        public Neural_Network bestNetwork;
        public double Momentum;
        public double GlobalWeight;
        public double PersonalWeight;
        public Particle[] Particles;
        Random random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
        public double[][] TrainingInputs;
        public double[][] TrainingOutputs;
        public double[][] TestInputs;
        public double[][] TestOutputs;
        public List<double> AverageMovement = new List<double>();
        public int[] LayerSizes;
        public bool[] UseOpponent;
        public bool[] UseOffense;
        public int Resets = 0;
        public int[] InputStats;
        public int[] OutputStats;

        //
        // Constructor
        public Swarm(int nParticles, double W, double mG, double mP, int[] LayerInfo, int[] inputTypes, int[] outputTypes, bool[] inUseOpponent, bool[] inUseOffense, double[][] trainIn, double[][] trainOut, double[][] testIn, double[][] testOut)
        {
            // Set parameters
            Momentum = W;
            GlobalWeight = mG;
            PersonalWeight = mP;
            LayerSizes = LayerInfo;
            InputStats = inputTypes;
            OutputStats = outputTypes;
            UseOpponent = inUseOpponent;
            UseOffense = inUseOffense;
            TrainingInputs = trainIn;
            TrainingOutputs = trainOut;
            TestInputs = testIn;
            TestOutputs = testOut;

            // Initialize particles
            Particles = new Particle[nParticles];
            for (int i = 0; i < nParticles; i++)
                Particles[i] = new Particle(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, random);

            // Get fitnesses and find best initial particle
            GetParticleFitnesses(-1);
        }

        //
        // Updates all fitnesses and checks for new global best
        public void GetParticleFitnesses(int it)
        {
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i].GetFitness(TrainingInputs, TrainingOutputs);   // get fitness
                if (Particles[i].PersonalBest < GlobalBest)                 // check for new global best
                {
                    GlobalBest = Particles[i].PersonalBest;
                    GlobalBestHistory.Add(GlobalBest);
                    GlobalBestUpdateIteration.Add(it);
                    bestNetwork = Particles[i].bestNetwork.SetWeights();
                }
            }
        }

        //
        // Adjusts the weights of all particles in this swarm
        public void MoveParticles()
        {
            double tot = 0;
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i].MoveParticle(Momentum, GlobalWeight, PersonalWeight, bestNetwork);
                tot += Particles[i].AverageMovement;
            }
            AverageMovement.Add(tot / Particles.Length);
        }

        //
        // Resets the particle weights when there is too little movement
        public void ResetParticles()
        {
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i].currNetwork = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, random, Resets);
                Particles[i].stepNetwork = new Neural_Network(LayerSizes, InputStats, OutputStats, UseOpponent, UseOffense, random);
            }
            Resets++;
        }

        //
        // Get MSE of global best network to test data
        public double BestTestFitness()
        {
            double error = 0;   // Initialize error to 0

            // Get error
            int row = 0;
            foreach (double[] line in TestInputs)
            {
                double[] output = bestNetwork.Think(line);
                int col = 0;
                foreach (double val in output)
                {
                    error += Math.Abs((val - TestOutputs[row][col]) * (val - TestOutputs[row][col]));
                    col++;
                }
                row++;
            }
            return error / TestInputs.Length;
        }

        //
        // Get MSE of global best network to training data
        public double BestTrainingFitness()
        {
            double error = 0;   // Initialize error to 0

            // Get error
            int row = 0;
            foreach (double[] line in TrainingInputs)
            {
                double[] output = bestNetwork.Think(line);
                int col = 0;
                foreach (double val in output)
                {
                    error += Math.Abs((val - TrainingOutputs[row][col]) * (val - TrainingOutputs[row][col]));
                    col++;
                }
                row++;
            }
            return error / TrainingInputs.Length;
        }

        //
        // Get MSE of global best network to all data
        public double BestTotalFitness()
        {
            double error = 0;   // Initialize error to 0

            // Get error
            int row = 0;
            foreach (double[] line in TestInputs)
            {
                double[] output = bestNetwork.Think(line);
                int col = 0;
                foreach (double val in output)
                {
                    error += Math.Abs((val - TestOutputs[row][col]) * (val - TestOutputs[row][col]));
                    col++;
                }
                row++;
            }
            row = 0;
            foreach (double[] line in TrainingInputs)
            {
                double[] output = bestNetwork.Think(line);
                int col = 0;
                foreach (double val in output)
                {
                    error += Math.Abs((val - TrainingOutputs[row][col]) * (val - TrainingOutputs[row][col]));
                    col++;
                }
                row++;
            }
            return error / (TrainingInputs.Length + TestInputs.Length);
        }
    }
}