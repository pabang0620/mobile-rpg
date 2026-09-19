using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Presentation.Login;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds Login.unity from scratch: title background/logo, an account-id
    /// InputField, a "게임 시작" button, and an error line. Mirrors
    /// SapphireSceneBuilder's "always start from a brand-new empty scene,
    /// idempotent" approach. Build Settings registration is Login's own
    /// responsibility to put at index 0 (docs/planning/01_PRODUCT.md's "첫
    /// 씬이 로그인" - see CharacterFlowSceneBuilder for the overall order).
    /// </summary>
    internal static class LoginSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/Login.unity";

        // Sized/centered to contain both AccountIdInput (anchoredPosition
        // (0,-20), size (360,72), so its own top/bottom edges are at
        // 16/-56) and StartButton (anchoredPosition (0,-140), size
        // (280,90), top/bottom edges at -95/-185) with a comfortable margin
        // on every side - matches LoginPortalFrame.png's own authored target
        // size (400x260) exactly, so no unstretched-corner distortion.
        private static readonly Vector2 PortalSize = new Vector2(400f, 260f);
        private const float PortalCenterY = -85f;

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();

            CharacterFlowUiScaffold.BuildFullScreenVideoBackground(canvasGo, "Assets/Sapphire/Art/Video/login_village_plaza_loop.mp4");

            Sprite logo = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleLogo.png");
            BuildLogo(canvasGo, logo);

            // 2026-09-16 (premium select/create/login rebuild): wraps the ID
            // input + start button in a single LoginPortalFrame.png backing
            // panel - layout of the two controls themselves is unchanged
            // (still anchoredPosition (0,-20)/(0,-140)), only a background
            // frame is added behind them, sized/centered to contain both
            // with room to spare (PortalCenterY/PortalSize below).
            Sprite portalFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/LoginPortalFrame.png");
            CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, portalFrame, "LoginPortal", new Vector2(0f, PortalCenterY), PortalSize);

            // 2026-09-16 (login input/button redesign task): swapped from the
            // shared InputFieldFrame.png to a dedicated NicknameInputFieldV2.png
            // (dark-navy glass panel with a blue neon glow border) - Character
            // CreateSceneBuilder's own nickname field still uses
            // InputFieldFrame.png unchanged (grep-confirmed before this
            // swap), so that shared asset itself is untouched, only this one
            // call site's sprite reference changed. Layout (position/size)
            // below is unchanged - only the sprite differs.
            Sprite inputFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/NicknameInputFieldV2.png");
            InputField accountIdInput = BuildAccountIdInput(canvasGo, inputFrame);

            // 2026-09-16 (login input/button redesign task): swapped from
            // ButtonPrimary.png (warm brown hex-cut pill, grep-confirmed used
            // nowhere else so nothing else is affected) to the new
            // LoginStartButtonV2.png (blue neon hexagon, Normal/Pressed 2-cell
            // sheet - see HudArtImportConfigurator... actually
            // CharacterFlowArtImportConfigurator.ConfigureTwoCellButton, same
            // pattern as ButtonSelectV2/ButtonDeleteV2/ButtonCreateV2).
            // Button.transition is switched from the Selectable default
            // (ColorTint) to SpriteSwap so pressedSprite actually renders on
            // press - the shared CharacterFlowUiScaffold.BuildLabeledButton
            // helper doesn't wire SpriteSwap itself (every other screen using
            // it is fine with the default color-tint feedback), so this is
            // done locally here rather than changing that shared helper for
            // every other button using it.
            Sprite startButtonSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/LoginStartButtonV2.png", "Normal");
            Sprite startButtonPressedSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/LoginStartButtonV2.png", "Pressed");
            Button startButton = CharacterFlowUiScaffold.BuildLabeledButton(canvasGo, startButtonSprite, "StartButton", new Vector2(0f, -140f), new Vector2(280f, 90f), "게임 시작", fontSize: 28);
            startButton.transition = Selectable.Transition.SpriteSwap;
            SpriteState startButtonSpriteState = startButton.spriteState;
            startButtonSpriteState.pressedSprite = startButtonPressedSprite;
            startButton.spriteState = startButtonSpriteState;

            Text errorText = CharacterFlowUiScaffold.BuildLabel(canvasGo, "ErrorText", new Vector2(0f, -195f), new Vector2(500f, 32f), string.Empty, fontSize: 20);
            errorText.color = new Color(1f, 0.5f, 0.5f);

            var controllerGo = new GameObject("LoginScreenController", typeof(LoginScreenController));
            var controller = controllerGo.GetComponent<LoginScreenController>();
            VillageHubUiBuilder.AssignField(controller, "accountIdInput", accountIdInput);
            VillageHubUiBuilder.AssignField(controller, "startButton", startButton);
            VillageHubUiBuilder.AssignField(controller, "errorText", errorText);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.Exception("Login scene save failed");
            }

            BuildSettingsSceneRegistrar.RegisterFirst(ScenePath);
        }

        private static void BuildLogo(GameObject canvasGo, Sprite logoSprite)
        {
            var logoGo = new GameObject("Logo", typeof(Image));
            logoGo.transform.SetParent(canvasGo.transform, false);
            var rect = logoGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(480f, 180f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            var image = logoGo.GetComponent<Image>();
            image.sprite = logoSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private static InputField BuildAccountIdInput(GameObject canvasGo, Sprite frameSprite)
        {
            Image frame = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, frameSprite, "AccountIdInput", new Vector2(0f, -20f), new Vector2(360f, 72f));
            GameObject frameGo = frame.gameObject;
            var inputField = frameGo.AddComponent<InputField>();

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(frameGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.06f, 0f);
            textRect.anchorMax = new Vector2(0.94f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.color = Color.white;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var placeholderGo = new GameObject("Placeholder", typeof(Text));
            placeholderGo.transform.SetParent(frameGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.06f, 0f);
            placeholderRect.anchorMax = new Vector2(0.94f, 1f);
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = VillageHubUiBuilder.LoadKoreanFont();
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            placeholder.fontSize = 26;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.text = "아이디 입력";

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.characterLimit = Sapphire.Domain.Account.AccountIdValidator.MaxLength;

            return inputField;
        }
    }
}
