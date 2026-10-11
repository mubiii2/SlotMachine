using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Core;
using SlotMachine.RNG;
using UnityEngine;

namespace SlotMachine.Reels
{
    /// <summary>
    /// Coordinates all reels in the slot machine, managing staggered spin-stops,
    /// anticipation delays, and collecting the final symbol matrix.
    /// </summary>
    public class ReelManager : MonoBehaviour
    {
        [Header("Reel References")]
        [SerializeField] private List<Reel> reels = new List<Reel>();

        [Header("Timing Configuration")]
        [Tooltip("Minimum duration in seconds before the first reel begins stopping")]
        [SerializeField] private float minSpinDuration = 1.0f;

        [Tooltip("Delay in seconds between successive reel stops")]
        [SerializeField] private float reelStopInterval = 0.35f;

        public event Action<int> OnSingleReelStopped;
        public event Action<SymbolId[,]> OnAllReelsStopped;

        public bool IsSpinning { get; private set; }

        private SymbolId[,] _finalOutcome; // [reelIndex, rowIndex] where row 0 = Top, 1 = Middle, 2 = Bottom
        private int _stoppedReelsCount;
        private Coroutine _sequenceCoroutine;

        public void Initialize(IReadOnlyList<SymbolDefinition> symbols, IRandomNumberGenerator rng)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].Initialize(i, symbols, rng);
                reels[i].OnReelStopped += HandleReelStopped;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].OnReelStopped -= HandleReelStopped;
                }
            }
        }

        /// <summary>
        /// Begins spinning all reels and sequences their staggered stop according to the predetermined outcome.
        /// </summary>
        /// <param name="outcomeMatrix">Symbol matrix of shape [3 reels, 3 rows]</param>
        public void Spin(SymbolId[,] outcomeMatrix)
        {
            if (IsSpinning) return;

            _finalOutcome = outcomeMatrix;
            _stoppedReelsCount = 0;
            IsSpinning = true;

            // Clear previous highlight states
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].ClearHighlights();
                reels[i].StartSpin();
            }

            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(StopSequenceRoutine());
        }

        private IEnumerator StopSequenceRoutine()
        {
            // Allow reels to spin at high velocity for minimum duration
            yield return new WaitForSeconds(minSpinDuration);

            // Staggered stop commands: Reel 0, then Reel 1, then Reel 2
            for (int i = 0; i < reels.Count; i++)
            {
                var columnOutcome = new SymbolId[3];
                columnOutcome[0] = _finalOutcome[i, 0]; // Top
                columnOutcome[1] = _finalOutcome[i, 1]; // Middle
                columnOutcome[2] = _finalOutcome[i, 2]; // Bottom

                reels[i].RequestStop(columnOutcome);

                // Wait before triggering the next reel's stop
                if (i < reels.Count - 1)
                {
                    yield return new WaitForSeconds(reelStopInterval);
                }
            }
        }

        private void HandleReelStopped(int reelIndex, SymbolId[] visibleSymbols)
        {
            OnSingleReelStopped?.Invoke(reelIndex);
            _stoppedReelsCount++;

            if (_stoppedReelsCount >= reels.Count)
            {
                IsSpinning = false;
                _sequenceCoroutine = null;
                OnAllReelsStopped?.Invoke(_finalOutcome);
            }
        }

        /// <summary>
        /// Highlights winning symbols across reels.
        /// </summary>
        public void HighlightWinningSymbols(bool[,] winningMatrix)
        {
            for (int r = 0; r < reels.Count; r++)
            {
                bool[] col = new bool[3];
                col[0] = winningMatrix[r, 0];
                col[1] = winningMatrix[r, 1];
                col[2] = winningMatrix[r, 2];
                reels[r].HighlightSymbols(col);
            }
        }

        public void ClearHighlights()
        {
            for (int r = 0; r < reels.Count; r++)
            {
                reels[r].ClearHighlights();
            }
        }

        public void AddReel(Reel reel)
        {
            if (!reels.Contains(reel))
            {
                reels.Add(reel);
            }
        }
    }
}
