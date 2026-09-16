using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Presentation.CharacterFlow;

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
    ///
    /// 2026-09-16 (widen menu + system section task): menuIds/quitConfirmDialog
    /// added so "character_select"/"quit" can get real behavior instead of the
    /// generic Select() placeholder - this dispatch has to live here (a normal
    /// runtime MonoBehaviour.Awake, re-run every scene load) rather than as a
    /// direct Button.onClick.AddListener call in the Editor SceneBuilder,
    /// because AddListener calls made in an Editor-only build script are
    /// non-persistent UnityEvent listeners: they are never serialized by
    /// EditorSceneManager.SaveScene and so silently stop working the moment
    /// the saved scene is reloaded or shipped in a Player build. That exact
    /// bug is why the old CharacterSelectButton footer (wired that way, see
    /// git history of VillageHubMenuBuilder.BuildCharacterSelectButton) was
    /// already disconnected from Build() and had to be removed as dead code
    /// rather than reused as-is.
    ///
    /// 2026-09-16 (character_select click bug fix, docs/HANDOFF.md): the
    /// wiring above (id-based dispatch in a real runtime Awake) was already
    /// correct - the actual bug was one level down, in
    /// VillageHubMenuBuilder.BuildOdinMenuItem: every menu item's root
    /// GameObject had a Button but no Graphic of its own, and its Icon/Label
    /// children both had raycastTarget=false, so UnityEngine.UI's
    /// GraphicRaycaster had nothing to hit-test in that item's hierarchy at
    /// all - clicks fell through to the panel's own background Image
    /// instead, which has no click handler. That affected every item in the
    /// grid, not just "character_select"/"quit" (those two just happened to
    /// be the ones exercised right after this session's menu widen). Fixed
    /// there by giving each item root its own invisible full-cell Image as a
    /// raycast target; this class needed no change.
    /// </summary>
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button[] menuButtons;
        [SerializeField] private string[] menuLabels;
        [SerializeField] private bool[] menuAvailable;
        [SerializeField] private string[] menuIds;
        [SerializeField] private SimpleMessagePanel messagePanel;
        [SerializeField] private ConfirmDialog quitConfirmDialog;

        private const string CharacterSelectId = "character_select";
        private const string QuitId = "quit";
        private const string CharacterSelectSceneName = "CharacterSelect";

        public void Configure(GameObject root, Button opener, Button[] buttons, string[] labels, bool[] available, string[] ids, SimpleMessagePanel messages, ConfirmDialog quitDialog)
        {
            panelRoot = root;
            openButton = opener;
            menuButtons = buttons;
            menuLabels = labels;
            menuAvailable = available;
            menuIds = ids;
            messagePanel = messages;
            quitConfirmDialog = quitDialog;
        }

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (menuButtons != null)
            {
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    if (menuButtons[i] == null) continue;
                    int index = i;
                    string id = menuIds != null && index < menuIds.Length ? menuIds[index] : null;
                    bool isAvailable = menuAvailable != null && index < menuAvailable.Length && menuAvailable[index];

                    if (id == CharacterSelectId)
                    {
                        menuButtons[i].onClick.AddListener(SelectCharacterSelect);
                    }
                    else if (id == QuitId)
                    {
                        menuButtons[i].onClick.AddListener(SelectQuit);
                    }
                    else if (isAvailable)
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

        private void SelectCharacterSelect()
        {
            Close();
            SceneManager.LoadScene(CharacterSelectSceneName);
        }

        private void SelectQuit()
        {
            Close();
            if (quitConfirmDialog != null)
            {
                quitConfirmDialog.Show("게임 종료", "정말 게임을 종료하시겠습니까?", QuitGame);
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
