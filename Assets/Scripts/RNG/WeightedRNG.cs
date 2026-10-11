using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using SlotMachine.Core;

namespace SlotMachine.RNG
{
    /// <summary>
    /// Robust Random Number Generator implementation utilizing weighted probability distribution
    /// to simulate genuine casino reel stop behavior while maintaining fairness and mathematical predictability.
    /// </summary>
    public class WeightedRNG : IRandomNumberGenerator
    {
        private readonly System.Random _random;

        /// <summary>
        /// Initializes the RNG with high-entropy seed sourced from System.Security.Cryptography.
        /// </summary>
        public WeightedRNG()
        {
            byte[] seedBytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }
            int seed = BitConverter.ToInt32(seedBytes, 0);
            _random = new System.Random(seed);
        }

        /// <summary>
        /// Overload for deterministic seeding (ideal for automated testing or debugging specific combinations).
        /// </summary>
        public WeightedRNG(int seed)
        {
            _random = new System.Random(seed);
        }

        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        public int NextInt(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Selects a symbol from the provided list based on cumulative weight distribution.
        /// Higher weight corresponds to higher probability of appearance.
        /// </summary>
        public SymbolId GetRandomSymbol(IReadOnlyList<SymbolDefinition> symbols)
        {
            if (symbols == null || symbols.Count == 0)
            {
                throw new ArgumentException("Symbols list cannot be empty for RNG calculation.", nameof(symbols));
            }

            int totalWeight = 0;
            for (int i = 0; i < symbols.Count; i++)
            {
                totalWeight += Math.Max(1, symbols[i].rngWeight);
            }

            int roll = _random.Next(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < symbols.Count; i++)
            {
                cumulative += Math.Max(1, symbols[i].rngWeight);
                if (roll < cumulative)
                {
                    return symbols[i].id;
                }
            }

            return symbols[symbols.Count - 1].id;
        }

        /// <summary>
        /// Generates a full column of visible symbols for a reel (e.g., Top, Middle, Bottom).
        /// </summary>
        public SymbolId[] GenerateReelOutcome(IReadOnlyList<SymbolDefinition> symbols, int rowCount = 3)
        {
            var outcome = new SymbolId[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                outcome[i] = GetRandomSymbol(symbols);
            }
            return outcome;
        }
    }
}
