using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    /// <summary>
    /// Minimal message panel built from MessagePanelFrameGold.png / MenuButtonGold.png.
    /// SetText/SetTitleAndBody/Show/Close is the entire public contract.
    /// </summary>
    public class SimpleMessagePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [SerializeField] private Button closeButton;
        // 2026-09-16 Phase 3: optional title line above the body text, used by
        // MainMenuPanel's per-item sub-panel ("title = item label, body =
        // placeholder copy" - see VillageHubUiBuilder.BuildMessagePanel). Left
        // null for callers that only ever used the single-text form (none
        // currently do, but SetText is kept working standalone).
        [SerializeField] private Text titleText;

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
            if (titleText != null)
            {
                titleText.text = string.Empty;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void SetTitleAndBody(string title, string body)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (messageText != null)
            {
                messageText.text = body;
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
