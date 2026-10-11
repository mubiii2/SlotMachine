using System.Collections;
using System.Collections.Generic;
using SlotMachine.Audio;
using SlotMachine.Core;
using SlotMachine.Reels;
using SlotMachine.RNG;
using SlotMachine.UI;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// Master coordinator for the slot machine.
    /// Connects inputs, reel animation sequences, RNG outcomes, payout evaluation, and UI updates.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ReelManager reelManager;
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SoundManager soundManager;

        [Header("Symbol Definitions")]
        [SerializeField] private List<SymbolDefinition> symbolDefinitions = new List<SymbolDefinition>();

        [Header("Auto Spin Delay")]
        [SerializeField] private float autoSpinDelay = 1.2f;

        private IRandomNumberGenerator _rng;
        private PaylineEvaluator _evaluator;
        private SlotState _currentState = SlotState.Idle;
        private Coroutine _autoSpinCoroutine;

        private void Awake()
        {
            _rng = new WeightedRNG();
        }

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Fallback default definitions if not set via Inspector
            if (symbolDefinitions == null || symbolDefinitions.Count == 0)
            {
                CreateDefaultSymbolDefinitions();
            }

            _evaluator = new PaylineEvaluator(symbolDefinitions);

            if (reelManager != null)
            {
                reelManager.Initialize(symbolDefinitions, _rng);
                reelManager.OnSingleReelStopped += HandleSingleReelStopped;
                reelManager.OnAllReelsStopped += HandleAllReelsStopped;
            }

            if (uiManager != null)
            {
                uiManager.OnSpinRequested += RequestSpin;
                uiManager.OnBetPlusRequested += () => playerWallet?.IncreaseBet();
                uiManager.OnBetMinusRequested += () => playerWallet?.DecreaseBet();
                uiManager.OnMaxBetRequested += () => playerWallet?.SetMaxBet();
                uiManager.OnAutoSpinToggled += HandleAutoSpinToggled;
                uiManager.OnRefillConfirmed += () => playerWallet?.RefillBalance(1000);
            }

            if (playerWallet != null)
            {
                playerWallet.OnBalanceChanged += HandleBalanceChanged;
                playerWallet.OnBetChanged += HandleBetChanged;
                playerWallet.OnFreeSpinsChanged += HandleFreeSpinsChanged;

                uiManager?.UpdateBalance(playerWallet.CurrentBalance);
                uiManager?.UpdateBet(playerWallet.CurrentBet);
            }

            uiManager?.SetMessage("WELCOME! PLACE YOUR BET AND PULL LEVER TO SPIN!");
            uiManager?.SetWinDirect(0);
        }

        private void OnDestroy()
        {
            if (reelManager != null)
            {
                reelManager.OnSingleReelStopped -= HandleSingleReelStopped;
                reelManager.OnAllReelsStopped -= HandleAllReelsStopped;
            }
        }

        public void SetSymbolDefinitions(List<SymbolDefinition> defs)
        {
            symbolDefinitions = defs;
        }

        private void CreateDefaultSymbolDefinitions()
        {
            symbolDefinitions = new List<SymbolDefinition>
            {
                new SymbolDefinition { id = SymbolId.Seven, displayName = "Lucky 7", threeMatchMultiplier = 100, rngWeight = 8, isWild = false, isBonus = false },
                new SymbolDefinition { id = SymbolId.Bell, displayName = "Bell", threeMatchMultiplier = 50, rngWeight = 16, isWild = false, isBonus = true },
                new SymbolDefinition { id = SymbolId.Bar, displayName = "BAR", threeMatchMultiplier = 25, rngWeight = 26, isWild = false, isBonus = false },
                new SymbolDefinition { id = SymbolId.Cherry, displayName = "Cherry", threeMatchMultiplier = 10, twoMatchMultiplier = 2, rngWeight = 38, isWild = false, isBonus = false },
                new SymbolDefinition { id = SymbolId.Wild, displayName = "Wild Star", threeMatchMultiplier = 150, rngWeight = 6, isWild = true, isBonus = false }
            };
        }

        /// <summary>
        /// Initiates a spin if current state allows and player has sufficient balance.
        /// </summary>
        public void RequestSpin()
        {
            if (_currentState != SlotState.Idle) return;

            if (playerWallet != null && !playerWallet.CanAffordSpin())
            {
                uiManager?.SetMessage("OUT OF CREDITS! REFILL TO CONTINUE PLAYING.", Color.red);
                uiManager?.ShowRefillModal();
                uiManager?.StopAutoSpin();
                return;
            }

            StartCoroutine(ExecuteSpinRoutine());
        }

        private IEnumerator ExecuteSpinRoutine()
        {
            _currentState = SlotState.Spinning;
            uiManager?.SetControlsInteractable(false);
            reelManager?.ClearHighlights();

            // Deduct bet from player's balance
            playerWallet?.TryDeductBet();

            // Visual feedback: animate lever
            uiManager?.TriggerLeverVisual();

            if (playerWallet != null && playerWallet.IsInFreeSpins)
            {
                uiManager?.SetMessage($"FREE SPIN IN PROGRESS! ({playerWallet.FreeSpinsRemaining} REMAINING)");
            }
            else
            {
                uiManager?.SetMessage("SPINNING... GOOD LUCK!");
            }

            // Generate RNG outcome for 3 reels x 3 rows
            var outcomeMatrix = new SymbolId[3, 3];
            for (int reel = 0; reel < 3; reel++)
            {
                for (int row = 0; row < 3; row++)
                {
                    outcomeMatrix[reel, row] = _rng.GetRandomSymbol(symbolDefinitions);
                }
            }

            // Audio: Start spin loop sound
            soundManager?.StartReelSpinSound();

            // Spin reels with the predetermined outcome
            reelManager?.Spin(outcomeMatrix);

            yield break;
        }

        private void HandleSingleReelStopped(int reelIndex)
        {
            soundManager?.PlayReelStop();
        }

        private void HandleAllReelsStopped(SymbolId[,] outcomeMatrix)
        {
            soundManager?.StopReelSpinSound();
            StartCoroutine(EvaluateOutcomeRoutine(outcomeMatrix));
        }

        private IEnumerator EvaluateOutcomeRoutine(SymbolId[,] outcomeMatrix)
        {
            _currentState = SlotState.Evaluating;

            int globalMultiplier = (playerWallet != null && playerWallet.IsInFreeSpins) ? 2 : 1;
            int bet = playerWallet != null ? playerWallet.CurrentBet : 10;

            SpinEvaluationResult result = _evaluator.Evaluate(outcomeMatrix, bet, globalMultiplier);

            if (result.hasWin)
            {
                // Highlight winning symbols on the reels
                reelManager?.HighlightWinningSymbols(result.winningCoordinates);

                // Play celebratory win audio
                soundManager?.PlayWin(result.isJackpot);

                // Animate win ticker on UI
                uiManager?.AnimateWinAmount(result.totalPayout, 1.2f);
                playerWallet?.AddWinnings(result.totalPayout);

                if (result.isJackpot)
                {
                    uiManager?.ShowJackpotBanner(true);
                    uiManager?.SetMessage($"*** MEGA JACKPOT! WON {result.totalPayout:N0} CREDITS! ***", Color.yellow);
                }
                else
                {
                    string lineSummary = result.winningLines.Count > 0 ? result.winningLines[0].lineName : "WIN";
                    uiManager?.SetMessage($"WINNER! {result.winningLines.Count} LINE(S) HIT! +{result.totalPayout:N0} CREDITS!", Color.green);
                }

                // Check for Free Spins bonus trigger
                if (result.triggersFreeSpins)
                {
                    playerWallet?.AddFreeSpins(result.freeSpinsAwarded);
                    uiManager?.SetFreeSpinsBanner(true, playerWallet.FreeSpinsRemaining);
                    uiManager?.SetMessage($"BONUS ACTIVATED! {result.freeSpinsAwarded} FREE SPINS AWARDED (2X WINS)!", Color.cyan);
                }

                yield return new WaitForSeconds(1.4f);
            }
            else
            {
                uiManager?.SetMessage("NO WIN THIS TIME. SPIN AGAIN!");
                yield return new WaitForSeconds(0.4f);
            }

            uiManager?.ShowJackpotBanner(false);

            // Update free spins UI
            if (playerWallet != null && playerWallet.IsInFreeSpins)
            {
                uiManager?.SetFreeSpinsBanner(true, playerWallet.FreeSpinsRemaining);
            }
            else
            {
                uiManager?.SetFreeSpinsBanner(false);
            }

            _currentState = SlotState.Idle;
            uiManager?.SetControlsInteractable(true);

            // Process auto spin if active
            if (uiManager != null && uiManager.IsAutoSpinActive)
            {
                yield return new WaitForSeconds(autoSpinDelay);
                if (uiManager.IsAutoSpinActive && _currentState == SlotState.Idle)
                {
                    RequestSpin();
                }
            }
        }

        private void HandleAutoSpinToggled()
        {
            if (uiManager != null && uiManager.IsAutoSpinActive && _currentState == SlotState.Idle)
            {
                RequestSpin();
            }
        }

        private void HandleBalanceChanged(int balance)
        {
            uiManager?.UpdateBalance(balance);
        }

        private void HandleBetChanged(int bet)
        {
            uiManager?.UpdateBet(bet);
        }

        private void HandleFreeSpinsChanged(int remaining)
        {
            uiManager?.SetFreeSpinsBanner(remaining > 0, remaining);
        }
    }
}
