using System;
using UnityEngine;

namespace SlotMachine.Core
{
    /// <summary>
    /// Configurable data model defining a single slot symbol, its visuals,
    /// payout multiplier, and occurrence weight in the RNG strip.
    /// </summary>
    [Serializable]
    public class SymbolDefinition
    {
        [Tooltip("Unique identifier for this symbol")]
        public SymbolId id;

        [Tooltip("Display name shown in UI and paytable")]
        public string displayName;

        [Tooltip("Visual 2D sprite associated with this symbol")]
        public Sprite sprite;

        [Tooltip("Payout multiplier rewarded for matching 3 of this symbol")]
        public int threeMatchMultiplier = 10;

        [Tooltip("Payout multiplier for 2 matching cherries (scatter/partial match)")]
        public int twoMatchMultiplier = 0;

        [Tooltip("Relative probability weight for RNG selection (Higher = more frequent)")]
        [Range(1, 100)]
        public int rngWeight = 20;

        [Tooltip("Whether this symbol functions as a Wild symbol substituting other symbols")]
        public bool isWild = false;

        [Tooltip("Whether this symbol triggers bonus free spins")]
        public bool isBonus = false;
    }
}
