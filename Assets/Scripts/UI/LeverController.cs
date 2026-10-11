using System;
using System.Collections;
using SlotMachine.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Interactive mechanical lever on the side of the slot machine.
    /// Supports user click/drag interaction with realistic spring-loaded animation.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LeverController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Sprites")]
        [SerializeField] private Sprite leverUpSprite;
        [SerializeField] private Sprite leverDownSprite;

        [Header("Animation Settings")]
        [SerializeField] private float pullDuration = 0.12f;
        [SerializeField] private float holdDuration = 0.15f;
        [SerializeField] private float returnDuration = 0.18f;

        public event Action OnLeverPulled;

        public bool IsInteractable { get; set; } = true;

        private Image _leverImage;
        private bool _isPulling;
        private Coroutine _pullCoroutine;

        private void Awake()
        {
            _leverImage = GetComponent<Image>();
            if (leverUpSprite != null && _leverImage != null)
            {
                _leverImage.sprite = leverUpSprite;
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

            // Switch to pulled-down sprite
            if (_leverImage != null && leverDownSprite != null)
            {
                _leverImage.sprite = leverDownSprite;
            }

            yield return new WaitForSeconds(holdDuration);

            // Return to up position
            if (_leverImage != null && leverUpSprite != null)
            {
                _leverImage.sprite = leverUpSprite;
            }

            yield return new WaitForSeconds(returnDuration);
            _isPulling = false;
            _pullCoroutine = null;
        }

        public void SetSprites(Sprite up, Sprite down)
        {
            leverUpSprite = up;
            leverDownSprite = down;
            if (_leverImage != null && leverUpSprite != null)
            {
                _leverImage.sprite = leverUpSprite;
            }
        }
    }
}
