using System;
using System.Collections;
using SlotMachine.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Interactive mechanical lever located on the right side of the slot machine cabinet.
    /// Sized specifically to the lever handle dimensions (92x270) with spring-loaded animation.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LeverController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Sprites")]
        [SerializeField] private Sprite leverUpSprite;
        [SerializeField] private Sprite leverDownSprite;

        [Header("Positions & Sizing")]
        [SerializeField] private Vector2 upPosition = new Vector2(310.5f, -118.5f);
        [SerializeField] private Vector2 upSize = new Vector2(92f, 270f);
        [SerializeField] private Vector2 downPosition = new Vector2(310.5f, -281.5f);
        [SerializeField] private Vector2 downSize = new Vector2(92f, 60f);

        [Header("Animation Settings")]
        [SerializeField] private float pullDuration = 0.12f;
        [SerializeField] private float holdDuration = 0.15f;
        [SerializeField] private float returnDuration = 0.18f;

        public event Action OnLeverPulled;

        public bool IsInteractable { get; set; } = true;

        private Image _leverImage;
        private RectTransform _rectTransform;
        private bool _isPulling;
        private Coroutine _pullCoroutine;

        private void Awake()
        {
            _leverImage = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();

            SetToUpPosition();
        }

        public void SetToUpPosition()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = upPosition;
                _rectTransform.sizeDelta = upSize;
            }
            if (_leverImage != null && leverUpSprite != null)
            {
                _leverImage.sprite = leverUpSprite;
                _leverImage.preserveAspect = true;
            }
        }

        public void SetToDownPosition()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = downPosition;
                _rectTransform.sizeDelta = downSize;
            }
            if (_leverImage != null && leverDownSprite != null)
            {
                _leverImage.sprite = leverDownSprite;
                _leverImage.preserveAspect = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable || _isPulling) return;
            PullLever();
        }

        /// <summary>
        /// Programmatically pulls the lever (e.g. when Spin button is clicked).
        /// </summary>
        public void PullLever()
        {
            if (_isPulling) return;
            if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
            _pullCoroutine = StartCoroutine(AnimateLeverRoutine());
        }

        private IEnumerator AnimateLeverRoutine()
        {
            _isPulling = true;
            SoundManager.Instance?.PlayLeverPull();
            OnLeverPulled?.Invoke();

            // Switch to pulled-down state
            SetToDownPosition();

            yield return new WaitForSeconds(holdDuration);

            // Return to up state with quick spring
            SetToUpPosition();

            yield return new WaitForSeconds(returnDuration);
            _isPulling = false;
            _pullCoroutine = null;
        }

        public void Configure(Sprite up, Sprite down, Vector2 posUp, Vector2 sizeUp, Vector2 posDown, Vector2 sizeDown)
        {
            leverUpSprite = up;
            leverDownSprite = down;
            upPosition = posUp;
            upSize = sizeUp;
            downPosition = posDown;
            downSize = sizeDown;
            SetToUpPosition();
        }
    }
}
