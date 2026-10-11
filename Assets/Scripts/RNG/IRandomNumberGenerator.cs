using System.Collections.Generic;
using SlotMachine.Core;

namespace SlotMachine.RNG
{
    /// <summary>
    /// Contract for RNG services providing fair and unpredictable symbol selection.
    /// Enables unit testing, deterministic mocking, and modular RNG swap.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// Selects a random symbol from the available pool using weighted distribution.
        /// </summary>
        SymbolId GetRandomSymbol(IReadOnlyList<SymbolDefinition> symbols);

        /// <summary>
        /// Generates a randomized 3-symbol column outcome for a reel (Top, Middle, Bottom).
        /// </summary>
        SymbolId[] GenerateReelOutcome(IReadOnlyList<SymbolDefinition> symbols, int rowCount = 3);

        /// <summary>
        /// Generates a float between 0.0 and 1.0.
        /// </summary>
        float NextFloat();

        /// <summary>
        /// Generates an integer in range [min, max).
        /// </summary>
        int NextInt(int min, int max);
    }
}
