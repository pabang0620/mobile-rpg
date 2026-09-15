using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    /// <summary>
    /// Thin controller for the Odin-style menu panel (2026-09-16 Phase 3):
    /// binds MenuCatalog data (already turned into buttons by
    /// VillageHubUiBuilder.BuildOdinMenu) to click events and the open/close
    /// toggle. It owns no layout - the grid, icons, lock badges and gray
    /// tinting are all built once by the Editor tool and just handed here as
    /// plain Button[]/label[] arrays, so this class stays a small event-wiring
    /// layer instead of a God class that also builds UI.
    /// </summary>
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button[] menuButtons;
        [SerializeField] private string[] menuLabels;
        [SerializeField] private bool[] menuAvailable;
        [SerializeField] private SimpleMessagePanel messagePanel;

        public void Configure(GameObject root, Button opener, Button[] buttons, string[] labels, bool[] available, SimpleMessagePanel messages)
        {
            panelRoot = root;
            openButton = opener;
            menuButtons = buttons;
            menuLabels = labels;
            menuAvailable = available;
            messagePanel = messages;
        }

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (menuButtons != null)
            {
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    int index = i;
                    bool isAvailable = menuAvailable != null && index < menuAvailable.Length && menuAvailable[index];
                    if (menuButtons[i] != null && isAvailable)
                    {
                        menuButtons[i].onClick.AddListener(() => Select(index));
                    }
                }
            }

            // Debug hook (task spec, not REMEDIATION_PLAN.md - that doc's
            // unnumbered Phase 3 bullet list has no "item 7"; this exact
            // requirement, including the -sapphire-open-menu flag name, comes
            // from the session's task instructions): launching with
            // -sapphire-open-menu opens the panel immediately so a
            // screenshot/verification pass can see the menu state without a
            // click. Intentionally unconditional (no dev-build/editor gate) -
            // its entire purpose is to be reachable from a real player build's
            // command line for verification. Kept to 5 lines including this
            // comment's code, per spec.
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-sapphire-open-menu") { Open(); break; }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle() { if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf); }
        public void Open() { if (panelRoot != null) panelRoot.SetActive(true); }
        public void Close() { if (panelRoot != null) panelRoot.SetActive(false); }

        private void Select(int index)
        {
            string label = menuLabels != null && index < menuLabels.Length ? menuLabels[index] : "메뉴";
            Close();
            if (messagePanel != null)
            {
                messagePanel.SetTitleAndBody(label, "이 기능은 다음 슬라이스에서 연결됩니다.");
                messagePanel.Show();
            }
        }
    }
}
