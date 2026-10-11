using System;
using System.Collections;
using SlotMachine.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Central manager for slot machine UI: balances, bets, win banners,
    /// counting tickers, modals, and user control buttons.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Display Text Fields")]
        [SerializeField] private Text balanceText;
        [SerializeField] private Text betText;
        [SerializeField] private Text winText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text freeSpinsText;

        [Header("Control Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button maxBetButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private Button paytableButton;

        [Header("Panels and Overlays")]
        [SerializeField] private GameObject freeSpinsBanner;
        [SerializeField] private GameObject jackpotBanner;
        [SerializeField] private PaytableModal paytableModal;
        [SerializeField] private GameObject refillModal;
        [SerializeField] private Button refillYesButton;
        [SerializeField] private Button refillNoButton;

        [Header("Lever Reference")]
        [SerializeField] private LeverController leverController;

        public event Action OnSpinRequested;
        public event Action OnBetPlusRequested;
        public event Action OnBetMinusRequested;
        public event Action OnMaxBetRequested;
        public event Action OnAutoSpinToggled;
        public event Action OnRefillConfirmed;

        public bool IsAutoSpinActive { get; private set; }

        private Coroutine _winTickerCoroutine;
        private int _displayedWin;

        private void Awake()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (spinButton != null) spinButton.onClick.AddListener(HandleSpinClick);
            if (betPlusButton != null) betPlusButton.onClick.AddListener(HandleBetPlusClick);
            if (betMinusButton != null) betMinusButton.onClick.AddListener(HandleBetMinusClick);
            if (maxBetButton != null) maxBetButton.onClick.AddListener(HandleMaxBetClick);
            if (autoSpinButton != null) autoSpinButton.onClick.AddListener(HandleAutoSpinClick);
            if (paytableButton != null) paytableButton.onClick.AddListener(HandlePaytableClick);

            if (refillYesButton != null) refillYesButton.onClick.AddListener(() =>
            {
                SoundManager.Instance?.PlayButtonClick();
                HideRefillModal();
                OnRefillConfirmed?.Invoke();
            });

            if (refillNoButton != null) refillNoButton.onClick.AddListener(() =>
            {
                SoundManager.Instance?.PlayButtonClick();
                HideRefillModal();
            });

            if (leverController != null)
            {
                leverController.OnLeverPulled += HandleSpinClick;
            }
        }

        private void HandleSpinClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            OnSpinRequested?.Invoke();
        }

        private void HandleBetPlusClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            OnBetPlusRequested?.Invoke();
        }

        private void HandleBetMinusClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            OnBetMinusRequested?.Invoke();
        }

        private void HandleMaxBetClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            OnMaxBetRequested?.Invoke();
        }

        private void HandleAutoSpinClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            IsAutoSpinActive = !IsAutoSpinActive;
            UpdateAutoSpinButtonVisual();
            OnAutoSpinToggled?.Invoke();
        }

        private void HandlePaytableClick()
        {
            SoundManager.Instance?.PlayButtonClick();
            if (paytableModal != null) paytableModal.Show();
        }

        private void UpdateAutoSpinButtonVisual()
        {
            if (autoSpinButton != null)
            {
                var text = autoSpinButton.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = IsAutoSpinActive ? "AUTO: ON" : "AUTO SPIN";
                    text.color = IsAutoSpinActive ? Color.yellow : Color.white;
                }
            }
        }

        public void StopAutoSpin()
        {
            IsAutoSpinActive = false;
            UpdateAutoSpinButtonVisual();
        }

        public void UpdateBalance(int balance)
        {
            if (balanceText != null)
            {
                balanceText.text = $"{balance:N0}";
            }
        }

        public void UpdateBet(int bet)
        {
            if (betText != null)
            {
                betText.text = $"{bet:N0}";
            }
        }

        public void SetMessage(string message, Color? color = null)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = color ?? Color.white;
            }
        }

        public void SetControlsInteractable(bool interactable)
        {
            if (spinButton != null) spinButton.interactable = interactable;
            if (betPlusButton != null) betPlusButton.interactable = interactable;
            if (betMinusButton != null) betMinusButton.interactable = interactable;
            if (maxBetButton != null) maxBetButton.interactable = interactable;
            if (leverController != null) leverController.IsInteractable = interactable;
        }

        /// <summary>
        /// Animates the win number ticking upward with audible coin sounds.
        /// </summary>
        public void AnimateWinAmount(int targetWin, float duration = 1.0f)
        {
            if (_winTickerCoroutine != null) StopCoroutine(_winTickerCoroutine);
            _winTickerCoroutine = StartCoroutine(WinTickerRoutine(targetWin, duration));
        }

        public void SetWinDirect(int win)
        {
            _displayedWin = win;
            if (winText != null)
            {
                winText.text = $"{win:N0}";
            }
        }

        private IEnumerator WinTickerRoutine(int targetWin, float duration)
        {
            int startWin = _displayedWin;
            float elapsed = 0f;
            int lastCoinSoundVal = startWin;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                float easedT = 1f - (1f - t) * (1f - t);
                _displayedWin = Mathf.RoundToInt(Mathf.Lerp(startWin, targetWin, easedT));

                if (winText != null) winText.text = $"{_displayedWin:N0}";

                if (_displayedWin - lastCoinSoundVal >= Math.Max(1, (targetWin - startWin) / 10))
                {
                    SoundManager.Instance?.PlayCoinCount();
                    lastCoinSoundVal = _displayedWin;
                }

                yield return null;
            }

            _displayedWin = targetWin;
            if (winText != null) winText.text = $"{targetWin:N0}";
            _winTickerCoroutine = null;
        }

        public void SetFreeSpinsBanner(bool active, int remainingSpins = 0)
        {
            if (freeSpinsBanner != null)
            {
                freeSpinsBanner.SetActive(active);
            }
            if (freeSpinsText != null)
            {
                freeSpinsText.text = $"FREE SPINS: {remainingSpins} (2X MULTIPLIER)";
            }
        }

        public void ShowJackpotBanner(bool show)
        {
            if (jackpotBanner != null)
            {
                jackpotBanner.SetActive(show);
            }
        }

        public void ShowRefillModal()
        {
            if (refillModal != null) refillModal.SetActive(true);
        }

        public void HideRefillModal()
        {
            if (refillModal != null) refillModal.SetActive(false);
        }

        public void TriggerLeverVisual()
        {
            if (leverController != null)
            {
                leverController.PullLever();
            }
        }
    }
}
