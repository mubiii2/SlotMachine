using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Core;
using SlotMachine.RNG;
using UnityEngine;

namespace SlotMachine.Reels
{
    /// <summary>
    /// Controls a single vertical reel column, handling smooth ease-in anticipation,
    /// steady-state spinning, decelerating to target symbols, and elastic bounce settlement.
    /// </summary>
    public class Reel : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [SerializeField] private int reelIndex = 0;
        [Tooltip("Vertical spacing between adjacent symbol centers. Increase to add more breathing space.")]
        [SerializeField] private float rowHeight = 105f; // Increased spacing so symbols never overlap
        [SerializeField] private int visibleRows = 3; // Top, Middle, Bottom
        [SerializeField] private int totalSymbolSlots = 5; // 3 visible + 2 buffers (1 above, 1 below)

        [Header("Animation Dynamics")]
        [SerializeField] private float spinSpeed = 900f; // Pixels per second downward
        [SerializeField] private float anticipationDuration = 0.2f;
        [SerializeField] private float anticipationDistance = 25f; // Upward kickback before spin
        [SerializeField] private float bounceDistance = 18f; // Downward overshoot when locking
        [SerializeField] private float bounceDuration = 0.22f; // Settle time

        [Header("References")]
        [SerializeField] private RectTransform symbolsContainer;
        [SerializeField] private List<ReelSymbol> symbolSlots = new List<ReelSymbol>();

        public event Action<int, SymbolId[]> OnReelStopped;
        public event Action<int> OnReelLocking; // Fires the instant deceleration & latch begins

        public bool IsSpinning { get; private set; }
        public int ReelIndex => reelIndex;

        private IReadOnlyList<SymbolDefinition> _availableSymbols;
        private IRandomNumberGenerator _rng;
        private Coroutine _spinCoroutine;
        private SymbolId[] _targetOutcome; // [0: Top, 1: Middle, 2: Bottom]

        public void Initialize(int index, IReadOnlyList<SymbolDefinition> symbols, IRandomNumberGenerator rng, List<ReelSymbol> slots = null)
        {
            reelIndex = index;
            _availableSymbols = symbols;
            _rng = rng;

            if (slots != null && slots.Count > 0)
            {
                symbolSlots = slots;
            }

            ResetPositions();
        }

        /// <summary>
        /// Resets the symbols to their default vertical resting positions.
        /// Slot indices:
        /// 0: Buffer Top (+140)
        /// 1: Visible Top (+70)
        /// 2: Visible Middle (0)
        /// 3: Visible Bottom (-70)
        /// 4: Buffer Bottom (-140)
        /// </summary>
        public void ResetPositions()
        {
            float halfSpan = (totalSymbolSlots - 1) * 0.5f;
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                float targetY = (halfSpan - i) * rowHeight;
                symbolSlots[i].RectTransform.anchoredPosition = new Vector2(0f, targetY);
                if (_availableSymbols != null && _availableSymbols.Count > 0)
                {
                    SymbolDefinition randomDef = GetSymbolDef(_rng != null ? _rng.GetRandomSymbol(_availableSymbols) : _availableSymbols[i % _availableSymbols.Count].id);
                    symbolSlots[i].SetSymbol(randomDef);
                }
            }
        }

        /// <summary>
        /// Begins spinning the reel. Reel will spin until RequestStop is invoked.
        /// </summary>
        public void StartSpin()
        {
            if (IsSpinning) return;
            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>
        /// Commands the reel to decelerate and land on the predetermined target outcome.
        /// </summary>
        public void RequestStop(SymbolId[] targetOutcome)
        {
            _targetOutcome = targetOutcome;
        }

        private IEnumerator SpinRoutine()
        {
            IsSpinning = true;
            _targetOutcome = null;

            // 1. Anticipation / Wind-up: Reel winds backward slightly (moving up)
            float elapsed = 0f;
            var initialPositions = new float[symbolSlots.Count];
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                initialPositions[i] = symbolSlots[i].RectTransform.anchoredPosition.y;
            }

            while (elapsed < anticipationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / anticipationDuration;
                // Smooth sinusoidal windup
                float offset = Mathf.Sin(t * Mathf.PI * 0.5f) * anticipationDistance;
                for (int i = 0; i < symbolSlots.Count; i++)
                {
                    symbolSlots[i].RectTransform.anchoredPosition = new Vector2(0f, initialPositions[i] + offset);
                }
                yield return null;
            }

            // 2. High-speed continuous spin phase
            float currentSpeed = 0f;
            float acceleration = spinSpeed / 0.25f; // Accelerate to full speed in 0.25s
            float bottomLimit = -(2.5f * rowHeight);
            float topWrapY = 2.5f * rowHeight;

            while (_targetOutcome == null)
            {
                if (currentSpeed < spinSpeed)
                {
                    currentSpeed = Mathf.Min(spinSpeed, currentSpeed + acceleration * Time.deltaTime);
                }

                MoveSlotsDownward(currentSpeed * Time.deltaTime, bottomLimit, topWrapY);
                yield return null;
            }

            // 3. Stop requested: Trigger audio immediately at the moment the reel latches/locks
            OnReelLocking?.Invoke(reelIndex);

            // Feed the target symbols: Slot 1 -> Top, Slot 2 -> Middle, Slot 3 -> Bottom
            // We align symbols so target outcome cleanly settles at resting positions.
            yield return StartCoroutine(SettleToTarget(_targetOutcome, currentSpeed));

            IsSpinning = false;
            _spinCoroutine = null;

            // Report visible symbols [Top, Middle, Bottom]
            var visibleSymbols = new SymbolId[3];
            visibleSymbols[0] = symbolSlots[1].CurrentSymbolId;
            visibleSymbols[1] = symbolSlots[2].CurrentSymbolId;
            visibleSymbols[2] = symbolSlots[3].CurrentSymbolId;

            OnReelStopped?.Invoke(reelIndex, visibleSymbols);
        }

        private void MoveSlotsDownward(float distance, float bottomLimit, float topWrapY)
        {
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                Vector2 pos = symbolSlots[i].RectTransform.anchoredPosition;
                pos.y -= distance;

                if (pos.y < bottomLimit)
                {
                    pos.y += symbolSlots.Count * rowHeight;
                    // Assign a new random symbol as it wraps to top
                    if (_availableSymbols != null && _rng != null)
                    {
                        SymbolId randId = _rng.GetRandomSymbol(_availableSymbols);
                        symbolSlots[i].SetSymbol(GetSymbolDef(randId));
                    }
                }
                symbolSlots[i].RectTransform.anchoredPosition = pos;
            }
        }

        private IEnumerator SettleToTarget(SymbolId[] targets, float currentSpeed)
        {
            // Position targets directly into slots [1: Top, 2: Middle, 3: Bottom]
            // and buffer slots [0: Buffer Top, 4: Buffer Bottom]
            symbolSlots[1].SetSymbol(GetSymbolDef(targets[0]));
            symbolSlots[2].SetSymbol(GetSymbolDef(targets[1]));
            symbolSlots[3].SetSymbol(GetSymbolDef(targets[2]));

            if (_availableSymbols != null && _rng != null)
            {
                symbolSlots[0].SetSymbol(GetSymbolDef(_rng.GetRandomSymbol(_availableSymbols)));
                symbolSlots[4].SetSymbol(GetSymbolDef(_rng.GetRandomSymbol(_availableSymbols)));
            }

            // Target resting positions:
            // Slot 0: +140
            // Slot 1: +70 (Top)
            // Slot 2:   0 (Middle)
            // Slot 3: -70 (Bottom)
            // Slot 4: -140
            float[] restY = { 2 * rowHeight, 1 * rowHeight, 0f, -1 * rowHeight, -2 * rowHeight };

            // Smoothly move from current positions to restY - bounceDistance (overshoot)
            float settleDuration = 0.15f;
            float elapsed = 0f;
            float[] startY = new float[symbolSlots.Count];
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                startY[i] = symbolSlots[i].RectTransform.anchoredPosition.y;
            }

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                // Ease out quad towards the overshoot position
                float easeOut = 1f - (1f - t) * (1f - t);

                for (int i = 0; i < symbolSlots.Count; i++)
                {
                    float targetOvershoot = restY[i] - bounceDistance;
                    float currentY = Mathf.Lerp(startY[i], targetOvershoot, easeOut);
                    symbolSlots[i].RectTransform.anchoredPosition = new Vector2(0f, currentY);
                }
                yield return null;
            }

            // Elastic bounce back from overshoot to exact resting positions
            elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);

                // Damped harmonic oscillation curve for realistic mechanical click
                float oscillation = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 3f);

                for (int i = 0; i < symbolSlots.Count; i++)
                {
                    float currentY = restY[i] - (bounceDistance * (1f - t)) + (bounceDistance * 0.35f * oscillation);
                    symbolSlots[i].RectTransform.anchoredPosition = new Vector2(0f, currentY);
                }
                yield return null;
            }

            // Snap accurately to exact resting coordinates
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                symbolSlots[i].RectTransform.anchoredPosition = new Vector2(0f, restY[i]);
            }
        }

        private SymbolDefinition GetSymbolDef(SymbolId id)
        {
            if (_availableSymbols == null) return null;
            for (int i = 0; i < _availableSymbols.Count; i++)
            {
                if (_availableSymbols[i].id == id) return _availableSymbols[i];
            }
            return _availableSymbols[0];
        }

        public void HighlightSymbols(bool[] highlights)
        {
            // Visible indices are 1, 2, 3 corresponding to Top, Middle, Bottom
            if (highlights == null || highlights.Length < 3) return;
            symbolSlots[1].SetHighlight(highlights[0]);
            symbolSlots[2].SetHighlight(highlights[1]);
            symbolSlots[3].SetHighlight(highlights[2]);
        }

        public void ClearHighlights()
        {
            for (int i = 0; i < symbolSlots.Count; i++)
            {
                symbolSlots[i].SetHighlight(false);
            }
        }
    }
}
