using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    /// <summary>
    /// Minimal message panel built from MessagePanelFrame.png / WideButton.png.
    /// SetText/Show/Close is the entire public contract.
    /// </summary>
    public class SimpleMessagePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void SetText(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
