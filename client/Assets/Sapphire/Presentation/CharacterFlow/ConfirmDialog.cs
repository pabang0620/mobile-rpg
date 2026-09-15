using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.CharacterFlow
{
    /// <summary>
    /// Small reusable Yes/No confirmation popup (e.g. "이 캐릭터를 삭제하시겠습니까?"
    /// on CharacterSelect). Kept separate from SimpleMessagePanel (VillageHub's
    /// single-button info popup) rather than adding a second button there, so
    /// VillageHub's existing message-panel wiring/behavior is untouched.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;

        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Close);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            }
        }

        public void Show(string title, string body, Action confirmed)
        {
            onConfirm = confirmed;
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Close()
        {
            onConfirm = null;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void HandleConfirmClicked()
        {
            Action confirmed = onConfirm;
            Close();
            confirmed?.Invoke();
        }
    }
}
