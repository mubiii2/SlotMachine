using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Modal dialog displaying paytable multipliers, symbol values, and bonus rules.
    /// </summary>
    public class PaytableModal : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }

        public void Show()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
