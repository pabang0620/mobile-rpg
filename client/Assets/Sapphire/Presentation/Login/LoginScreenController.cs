using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Domain.Account;
using Sapphire.Domain.Character;
using Sapphire.Presentation.Session;

namespace Sapphire.Presentation.Login
{
    /// <summary>
    /// Login screen: account id input + "게임 시작" (docs/planning/01_PRODUCT.md's
    /// "로컬 시작 흐름" - no password, no server auth). On success, signs the
    /// account into the session service and loads CharacterSelect.
    ///
    /// Also owns two debug fast-paths, checked in Awake before the normal
    /// click-driven flow:
    ///
    /// 1. -sapphire-class=mage|warrior (same on-command-line-args-loop
    ///    pattern MainMenuPanel's -sapphire-open-menu uses) skips straight to
    ///    VillageHub with a synthetic, unsaved character of that class -
    ///    useful for iterating on a class's village-scene wiring without
    ///    clicking through Login/CharacterSelect/CharacterCreate every time.
    /// 2. -sapphire-scene=&lt;SceneName&gt; (2026-09-15, screenshot verification
    ///    task) jumps straight to any scene by name instead of only
    ///    VillageHub - Login itself is scene index 0 so it's always the
    ///    first scene a player build loads, making it the one place a
    ///    command-line arg can intercept the flow before any scene-specific
    ///    Awake runs. Paired with -sapphire-account=&lt;id&gt; (below) so the
    ///    target scene has a real signed-in AccountId to read a roster from
    ///    (CharacterSelect/CharacterCreate/VillageHub all read
    ///    CharacterSessionService.Instance.AccountId) - without it, jumping
    ///    straight to CharacterSelect would look up an empty account's
    ///    (likely empty) roster. -sapphire-scene takes priority over
    ///    -sapphire-class if both are somehow passed together, since it is
    ///    the more general mechanism (can already reach VillageHub via
    ///    -sapphire-scene=VillageHub, just without the synthetic character).
    /// 3. -sapphire-account=&lt;id&gt; alone (no -sapphire-scene) just signs the
    ///    session into that account before falling through to the normal
    ///    Login UI flow - lets a tester pre-seed a roster file for a known
    ///    account id and still exercise the real Login click path afterward.
    /// </summary>
    public sealed class LoginScreenController : MonoBehaviour
    {
        private const string DebugArgPrefix = "-sapphire-class=";
        private const string SceneArgPrefix = "-sapphire-scene=";
        private const string AccountArgPrefix = "-sapphire-account=";

        [SerializeField] private InputField accountIdInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Text errorText;

        private void Awake()
        {
            string debugAccountId = FindArgValue(AccountArgPrefix);
            if (!string.IsNullOrEmpty(debugAccountId))
            {
                CharacterSessionService.Instance.SignIn(debugAccountId);
            }

            if (TryHandleSceneLaunch())
            {
                return;
            }

            if (TryHandleDebugLaunch())
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartClicked);
            }

            SetError(string.Empty);
        }

        private void HandleStartClicked()
        {
            string accountId = accountIdInput != null ? accountIdInput.text.Trim() : string.Empty;
            if (!AccountIdValidator.IsValid(accountId))
            {
                SetError($"아이디를 {AccountIdValidator.MinLength}~{AccountIdValidator.MaxLength}자로 입력하세요.");
                return;
            }

            CharacterSessionService.Instance.SignIn(accountId);
            SceneManager.LoadScene("CharacterSelect");
        }

        private void SetError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
            }
        }

        // Intentionally unconditional (no dev-build/editor gate), same
        // reasoning as MainMenuPanel's -sapphire-open-menu hook: its purpose
        // is to be reachable from a real player build's command line.
        private bool TryHandleDebugLaunch()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (!arg.StartsWith(DebugArgPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string className = arg.Substring(DebugArgPrefix.Length);
                if (!Enum.TryParse(className, ignoreCase: true, result: out CharacterClass characterClass))
                {
                    Debug.LogWarning($"Unknown -sapphire-class value '{className}', ignoring debug launch.");
                    return false;
                }

                if (string.IsNullOrEmpty(CharacterSessionService.Instance.AccountId))
                {
                    CharacterSessionService.Instance.SignIn("debug");
                }

                var debugSlot = new CharacterSlot("debug-" + characterClass, "디버그" + characterClass, characterClass, 1);
                CharacterSessionService.Instance.SelectCharacter(debugSlot);
                SceneManager.LoadScene("VillageHub");
                return true;
            }

            return false;
        }

        // 2026-09-15 (screenshot verification task): -sapphire-scene lets a
        // diagnostics run jump straight to any registered scene by name
        // (Login/CharacterSelect/CharacterCreate/VillageHub) without clicking
        // through the flow. Combined with the -sapphire-account handling in
        // Awake above (parsed first, so this scene load happens with the
        // account already signed in).
        private bool TryHandleSceneLaunch()
        {
            string sceneName = FindArgValue(SceneArgPrefix);
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(sceneName);
            return true;
        }

        private static string FindArgValue(string prefix)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return null;
        }
    }
}
