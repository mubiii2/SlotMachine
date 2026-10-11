using System;
using System.Collections.Generic;
using SlotMachine.Core;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    [Serializable]
    public class WinningLineInfo
    {
        public PaylineId paylineId;
        public string lineName;
        public SymbolId matchedSymbol;
        public int multiplier;
        public int payoutAmount;
        public Vector2Int[] symbolCoordinates; // (reelIndex, rowIndex)
    }

    [Serializable]
    public class SpinEvaluationResult
    {
        public bool hasWin;
        public int totalPayout;
        public int totalMultiplier;
        public bool isJackpot;
        public bool triggersFreeSpins;
        public int freeSpinsAwarded;
        public List<WinningLineInfo> winningLines = new List<WinningLineInfo>();
        public bool[,] winningCoordinates = new bool[3, 3];
    }

    /// <summary>
    /// Pure logic evaluator for detecting winning lines, calculating multipliers,
    /// handling Wild substitution, and evaluating bonus triggers across a 3x3 symbol grid.
    /// </summary>
    public class PaylineEvaluator
    {
        private readonly Dictionary<SymbolId, SymbolDefinition> _symbolDefs = new Dictionary<SymbolId, SymbolDefinition>();

        // 5 standard paylines coordinates: [reel, row] for 3 reels
        private static readonly Vector2Int[][] Paylines = new Vector2Int[][]
        {
            // Middle line (Row 1)
            new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            // Top line (Row 0)
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            // Bottom line (Row 2)
            new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) },
            // Diagonal Down (Top-Left to Bottom-Right)
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2) },
            // Diagonal Up (Bottom-Left to Top-Right)
            new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0) }
        };

        private static readonly string[] PaylineNames = { "Middle Row", "Top Row", "Bottom Row", "Diagonal Down", "Diagonal Up" };

        public PaylineEvaluator(IEnumerable<SymbolDefinition> definitions)
        {
            foreach (var def in definitions)
            {
                _symbolDefs[def.id] = def;
            }
        }

        /// <summary>
        /// Evaluates the 3x3 symbol matrix against all paylines.
        /// </summary>
        /// <param name="matrix">3x3 matrix where matrix[reel, row] is the visible symbol</param>
        /// <param name="betAmount">Current total bet placed</param>
        /// <param name="globalMultiplier">Multiplier (e.g., 2x during Free Spins)</param>
        /// <returns>Comprehensive evaluation result</returns>
        public SpinEvaluationResult Evaluate(SymbolId[,] matrix, int betAmount, int globalMultiplier = 1)
        {
            var result = new SpinEvaluationResult();
            int bonusSymbolCount = 0;

            // Count bonus symbols anywhere on the grid for scatter / free spins
            for (int reel = 0; reel < 3; reel++)
            {
                for (int row = 0; row < 3; row++)
                {
                    SymbolId id = matrix[reel, row];
                    if (_symbolDefs.TryGetValue(id, out var def) && def.isBonus)
                    {
                        bonusSymbolCount++;
                    }
                }
            }

            // 3 or more bonus symbols trigger 5 Free Spins
            if (bonusSymbolCount >= 3)
            {
                result.triggersFreeSpins = true;
                result.freeSpinsAwarded = 5;
            }

            // Evaluate each payline
            for (int lineIdx = 0; lineIdx < Paylines.Length; lineIdx++)
            {
                var coords = Paylines[lineIdx];
                SymbolId s0 = matrix[coords[0].x, coords[0].y];
                SymbolId s1 = matrix[coords[1].x, coords[1].y];
                SymbolId s2 = matrix[coords[2].x, coords[2].y];

                if (TryEvaluateLine(s0, s1, s2, out SymbolId matchedSymbol, out int multiplier))
                {
                    int linePayout = (betAmount * multiplier) * globalMultiplier;
                    result.hasWin = true;
                    result.totalMultiplier += multiplier * globalMultiplier;
                    result.totalPayout += linePayout;

                    if (matchedSymbol == SymbolId.Seven || matchedSymbol == SymbolId.Wild)
                    {
                        result.isJackpot = true;
                    }

                    var lineInfo = new WinningLineInfo
                    {
                        paylineId = (PaylineId)lineIdx,
                        lineName = PaylineNames[lineIdx],
                        matchedSymbol = matchedSymbol,
                        multiplier = multiplier * globalMultiplier,
                        payoutAmount = linePayout,
                        symbolCoordinates = coords
                    };

                    result.winningLines.Add(lineInfo);

                    // Mark coordinates for UI visual highlights
                    for (int c = 0; c < coords.Length; c++)
                    {
                        result.winningCoordinates[coords[c].x, coords[c].y] = true;
                    }
                }
            }

            return result;
        }

        private bool TryEvaluateLine(SymbolId s0, SymbolId s1, SymbolId s2, out SymbolId matchedSymbol, out int multiplier)
        {
            matchedSymbol = s0;
            multiplier = 0;

            bool isWild0 = _symbolDefs.TryGetValue(s0, out var def0) && def0.isWild;
            bool isWild1 = _symbolDefs.TryGetValue(s1, out var def1) && def1.isWild;
            bool isWild2 = _symbolDefs.TryGetValue(s2, out var def2) && def2.isWild;

            // Case A: 3 Wilds
            if (isWild0 && isWild1 && isWild2)
            {
                matchedSymbol = SymbolId.Wild;
                multiplier = def0 != null ? def0.threeMatchMultiplier : 150;
                return true;
            }

            // Determine candidate non-wild symbol for line match
            SymbolId target = isWild0 ? (isWild1 ? s2 : s1) : s0;

            bool match0 = isWild0 || s0 == target;
            bool match1 = isWild1 || s1 == target;
            bool match2 = isWild2 || s2 == target;

            // Check 3 matching symbols (accounting for Wild substitution)
            if (match0 && match1 && match2)
            {
                matchedSymbol = target;
                if (_symbolDefs.TryGetValue(target, out var targetDef))
                {
                    multiplier = targetDef.threeMatchMultiplier;
                }
                return true;
            }

            // Check Cherry 2-match rule (first two reels match cherries)
            if ((s0 == SymbolId.Cherry || isWild0) && (s1 == SymbolId.Cherry || isWild1))
            {
                matchedSymbol = SymbolId.Cherry;
                if (_symbolDefs.TryGetValue(SymbolId.Cherry, out var cherryDef) && cherryDef.twoMatchMultiplier > 0)
                {
                    multiplier = cherryDef.twoMatchMultiplier;
                    return true;
                }
            }

            return false;
        }
    }
}
