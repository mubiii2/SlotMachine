using SlotMachine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Reels
{
    /// <summary>
    /// Visual component representing a single symbol slot on the reel strip.
    /// Handles sprite rendering, sizing, and highlight animations.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ReelSymbol : MonoBehaviour
    {
        [SerializeField] private Image symbolImage;
        [SerializeField] private RectTransform rectTransform;

        public SymbolId CurrentSymbolId { get; private set; }
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private void Awake()
        {
            if (symbolImage == null)
            {
                symbolImage = GetComponent<Image>();
            }
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Updates the visual sprite and metadata for this symbol.
        /// </summary>
        public void SetSymbol(SymbolDefinition def)
        {
            if (def == null) return;
            CurrentSymbolId = def.id;
            if (symbolImage != null)
            {
                symbolImage.sprite = def.sprite;
                symbolImage.color = Color.white;
            }
        }

        /// <summary>
        /// Applies an optional pulsing/highlight effect for winning symbols.
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            if (symbolImage == null) return;
            symbolImage.color = isHighlighted ? new Color(1f, 1f, 0.4f, 1f) : Color.white;
        }
    }
}
