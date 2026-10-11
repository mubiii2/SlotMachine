using System;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// Manages player bankroll, current bet configuration, winning payouts,
    /// and free spins tracking.
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        [Header("Starting Economy")]
        [SerializeField] private int startingBalance = 1000;
        [SerializeField] private int defaultBet = 10;
        [SerializeField] private int[] betPresets = { 5, 10, 25, 50, 100 };

        public int CurrentBalance { get; private set; }
        public int CurrentBet { get; private set; }
        public int LastWinAmount { get; private set; }
        public int FreeSpinsRemaining { get; private set; }
        public bool IsInFreeSpins => FreeSpinsRemaining > 0;

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;
        public event Action<int> OnWinAwarded;
        public event Action<int> OnFreeSpinsChanged;

        private int _betIndex = 1; // Default to betPresets[1] = 10

        private void Awake()
        {
            CurrentBalance = startingBalance;
            if (betPresets != null && betPresets.Length > 0)
            {
                _betIndex = Mathf.Clamp(1, 0, betPresets.Length - 1);
                CurrentBet = betPresets[_betIndex];
            }
            else
            {
                CurrentBet = defaultBet;
            }
        }

        private void Start()
        {
            OnBalanceChanged?.Invoke(CurrentBalance);
            OnBetChanged?.Invoke(CurrentBet);
        }

        public bool CanAffordSpin()
        {
            if (IsInFreeSpins) return true; // Free spins do not deduct balance
            return CurrentBalance >= CurrentBet;
        }

        public bool TryDeductBet()
        {
            if (IsInFreeSpins)
            {
                FreeSpinsRemaining--;
                OnFreeSpinsChanged?.Invoke(FreeSpinsRemaining);
                return true;
            }

            if (CurrentBalance < CurrentBet)
            {
                return false;
            }

            CurrentBalance -= CurrentBet;
            OnBalanceChanged?.Invoke(CurrentBalance);
            return true;
        }

        public void AddWinnings(int payout)
        {
            LastWinAmount = payout;
            CurrentBalance += payout;
            OnBalanceChanged?.Invoke(CurrentBalance);
            OnWinAwarded?.Invoke(payout);
        }

        public void AddFreeSpins(int spins)
        {
            FreeSpinsRemaining += spins;
            OnFreeSpinsChanged?.Invoke(FreeSpinsRemaining);
        }

        public void IncreaseBet()
        {
            if (betPresets == null || betPresets.Length == 0) return;
            if (_betIndex < betPresets.Length - 1)
            {
                _betIndex++;
                CurrentBet = betPresets[_betIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        public void DecreaseBet()
        {
            if (betPresets == null || betPresets.Length == 0) return;
            if (_betIndex > 0)
            {
                _betIndex--;
                CurrentBet = betPresets[_betIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        public void SetMaxBet()
        {
            if (betPresets == null || betPresets.Length == 0) return;
            _betIndex = betPresets.Length - 1;
            CurrentBet = betPresets[_betIndex];
            OnBetChanged?.Invoke(CurrentBet);
        }

        public void RefillBalance(int amount = 500)
        {
            CurrentBalance += amount;
            OnBalanceChanged?.Invoke(CurrentBalance);
        }
    }
}
